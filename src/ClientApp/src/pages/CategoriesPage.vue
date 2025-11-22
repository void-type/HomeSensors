<script lang="ts" setup>
import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router';
import type {
  CategoryResponse,
  IItemSetOfIFailure,
  TemperatureDeviceResponse,
} from '@/api/data-contracts';
import type { HttpResponse } from '@/api/http-client';
import type { ModalParameters } from '@/models/ModalParameters';
import { onBeforeUnmount, onMounted, reactive } from 'vue';
import { onBeforeRouteLeave, onBeforeRouteUpdate } from 'vue-router';
import ApiHelpers from '@/models/ApiHelpers';
import useAppStore from '@/stores/appStore';
import useMessageStore from '@/stores/messageStore';

const appStore = useAppStore();
const messageStore = useMessageStore();
const api = ApiHelpers.client;

const data = reactive({
  categories: [] as Array<CategoryResponse>,
  errors: [] as Array<string>,
  originalCategories: new Map<number, string>(),
  hasDirtyCategories: false,
});

function trackOriginalState(category: CategoryResponse) {
  if (category.id !== undefined) {
    data.originalCategories.set(
      category.id,
      JSON.stringify({
        name: category.name,
        order: category.order,
      }),
    );
  }
}

function isCategoryDirty(category: CategoryResponse): boolean {
  if (category.id === 0) {
    return true;
  } // New categories are always dirty
  if (category.id === undefined) {
    return false;
  }

  const original = data.originalCategories.get(category.id);
  if (!original) {
    return false;
  }

  const current = JSON.stringify({
    name: category.name,
    order: category.order,
  });

  return original !== current;
}

function updateDirtyState() {
  data.hasDirtyCategories = data.categories.some(category => isCategoryDirty(category));
}

function handleBeforeUnload(event: BeforeUnloadEvent) {
  if (data.hasDirtyCategories) {
    event.preventDefault();

    event.returnValue = '';
    return '';
  }
  return null;
}

function onCategoryInput() {
  updateDirtyState();
}

async function getCategories() {
  try {
    const response = await api().categoriesGetAll();
    data.categories = response.data.slice().sort((a, b) => (a.order || 0) - (b.order || 0));
    // Track original state for dirty checking
    data.categories.forEach(category => trackOriginalState(category));
    updateDirtyState();
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function newCategory() {
  if (data.categories.findIndex(x => (x.id || 0) < 1) > -1) {
    return;
  }

  const currentMaxOrder = Math.max(1, ...data.categories.map(x => x.order ?? 1));
  const newOrder = Math.floor((currentMaxOrder + 10) / 10) * 10;

  const newCat = {
    id: 0,
    name: '',
    order: newOrder,
  };

  data.categories.unshift(newCat);
  updateDirtyState();
}

async function reallyDeleteCategory(category: TemperatureDeviceResponse) {
  if (category.id === null || typeof category.id === 'undefined') {
    return;
  }

  try {
    const response = await api().categoriesDelete({ id: category.id });
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }

    await getCategories();
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function deleteCategory(category: CategoryResponse) {
  const parameters: ModalParameters = {
    title: 'Delete category',
    description: 'Do you really want to delete this category?',
    okAction: () => reallyDeleteCategory(category),
  };

  appStore.showModal(parameters);
}

async function saveCategory(category: CategoryResponse) {
  data.errors = [];

  const request = {
    id: category.id,
    name: category.name,
    order: category.order,
  };

  try {
    const response = await api().categoriesSave(request);
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }

    // Update the category in place
    const isNewCategory = category.id === 0;
    if (isNewCategory) {
      // For new categories, we need to refetch to get the complete data with the new ID
      const tempIndex = data.categories.findIndex(c => c.id === 0);
      if (tempIndex >= 0) {
        // Remove the temporary entry
        data.categories.splice(tempIndex, 1);
        // Refresh the full list to get the new category with proper ID
        await getCategories();
      }
    } else {
      // Update existing category in place with the form data
      const existingIndex = data.categories.findIndex(c => c.id === category.id);
      if (existingIndex >= 0) {
        // Merge the updated data while preserving other properties
        data.categories[existingIndex] = {
          ...data.categories[existingIndex],
          ...request,
        };
        // Update original state tracking
        trackOriginalState(data.categories[existingIndex]);
        // Re-sort by order since categories are ordered by 'order' field
        data.categories.sort((a, b) => (a.order || 0) - (b.order || 0));
      }
    }

    updateDirtyState();
  } catch (error) {
    const response = error as HttpResponse<unknown, unknown>;
    messageStore.setApiFailureMessages(response);

    const failures = (response.error as IItemSetOfIFailure).items || [];
    failures.forEach(x => data.errors.push(`${x.uiHandle}-${category.id}`));
  }
}

function beforeRouteChange(
  to: RouteLocationNormalized,
  from: RouteLocationNormalized,
  next: NavigationGuardNext,
) {
  if (data.hasDirtyCategories) {
    const parameters: ModalParameters = {
      title: 'Unsaved changes',
      description: 'You have unsaved changes. Do you really want to leave?',
      okAction: () => next(),
      cancelAction: () => next(false),
    };
    appStore.showModal(parameters);
  } else {
    next();
  }
}

onBeforeRouteUpdate(beforeRouteChange);
onBeforeRouteLeave(beforeRouteChange);

onMounted(async () => {
  await getCategories();
  window.addEventListener('beforeunload', handleBeforeUnload);
});

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload);
});
</script>

<template>
  <div class="container-xxl">
    <h1 class="mt-3">
      Categories
    </h1>
    <div class="mt-4">
      <button class="btn btn-primary" @click="newCategory()">
        New
      </button>
      <div id="categoriesAccordion" class="accordion mt-4">
        <div v-for="category in data.categories" :key="category.id" class="accordion-item">
          <h2 :id="`heading-${category.id}`" class="accordion-header">
            <button
              class="accordion-button collapsed"
              type="button"
              data-bs-toggle="collapse"
              :data-bs-target="`#collapse-${category.id}`"
              :aria-expanded="false"
              :aria-controls="`collapse-${category.id}`"
            >
              <div class="d-flex align-items-center w-100">
                <span class="me-auto">
                  {{ category.name || "New category" }}
                  <span v-if="isCategoryDirty(category)" class="badge bg-warning text-dark ms-2">Unsaved</span>
                </span>
              </div>
            </button>
          </h2>
          <div
            :id="`collapse-${category.id}`"
            class="accordion-collapse collapse"
            :aria-labelledby="`heading-${category.id}`"
            data-bs-parent="#categoriesAccordion"
          >
            <div class="accordion-body">
              <div class="grid gap-sm">
                <div class="g-col-12 g-col-md-6 g-col-lg-4">
                  <label :for="`name-${category.id}`" class="form-label">Name</label>
                  <input
                    :id="`name-${category.id}`"
                    v-model="category.name"
                    required
                    type="text"
                    class="form-control form-control-sm"
                    :class="{
                      'is-invalid': data.errors.includes(`name-${category.id}`),
                    }"
                    @input="onCategoryInput"
                  >
                </div>
                <div class="g-col-12 g-col-md-6 g-col-lg-4">
                  <label :for="`order-${category.id}`" class="form-label">Order</label>
                  <input
                    :id="`order-${category.id}`"
                    v-model="category.order"
                    required
                    type="number"
                    class="form-control form-control-sm"
                    :class="{
                      'is-invalid': data.errors.includes(`order-${category.id}`),
                    }"
                    @input="onCategoryInput"
                  >
                </div>
                <div class="g-col-12">
                  <div class="btn-toolbar">
                    <button class="btn btn-sm btn-primary me-2" @click="saveCategory(category)">
                      Save
                    </button>
                    <button
                      v-if="category.id"
                      class="btn btn-sm btn-danger ms-auto"
                      @click="deleteCategory(category)"
                    >
                      Delete
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div v-if="data.categories.length < 1" class="text-center mt-4">
          No categories.
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped></style>
