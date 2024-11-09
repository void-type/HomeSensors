<script lang="ts" setup>
import useAppStore from '@/stores/appStore';
import type {
  TemperatureDeviceResponse,
  IItemSetOfIFailure,
  CategoryResponse,
} from '@/api/data-contracts';
import { onMounted, reactive } from 'vue';
import ApiHelpers from '@/models/ApiHelpers';
import type { HttpResponse } from '@/api/http-client';
import useMessageStore from '@/stores/messageStore';
import type { ModalParameters } from '@/models/ModalParameters';

const appStore = useAppStore();
const messageStore = useMessageStore();
const api = ApiHelpers.client;

const data = reactive({
  categories: [] as Array<CategoryResponse>,
  errors: [] as Array<string>,
});

async function getCategories() {
  try {
    const response = await api().categoriesGetAll();
    data.categories = response.data.slice().sort((a, b) => (a.order || 0) - (b.order || 0));
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function newCategory() {
  if (data.categories.findIndex((x) => (x.id || 0) < 1) > -1) {
    return;
  }

  const currentMaxOrder = Math.max(1, ...data.categories.map((x) => x.order ?? 1));

  const newOrder = Math.floor((currentMaxOrder + 10) / 10) * 10;

  data.categories.unshift({
    id: 0,
    name: '',
    order: newOrder,
  });
}

async function reallyDeleteCategory(category: TemperatureDeviceResponse) {
  if (category.id === null || typeof category.id === 'undefined') {
    return;
  }

  try {
    const response = await api().categoriesDelete(category.id);
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

    await getCategories();
  } catch (error) {
    const response = error as HttpResponse<unknown, unknown>;
    messageStore.setApiFailureMessages(response);

    const failures = (response.error as IItemSetOfIFailure).items || [];
    failures.forEach((x) => data.errors.push(`${x.uiHandle}-${category.id}`));
  }
}

onMounted(async () => {
  await getCategories();
});
</script>

<template>
  <button class="btn btn-primary" @click="newCategory()">New</button>
  <div class="grid mt-4">
    <div v-for="category in data.categories" :key="category.id" class="card g-col-12">
      <div class="card-body">
        <div class="grid gap-sm">
          <div v-if="!category.id" class="g-col-12">New category</div>
          <div class="g-col-12 g-col-md-6 g-col-lg-4">
            <label :for="`name-${category.id}`" class="form-label">Name</label>
            <input
              :id="`name-${category.id}`"
              v-model="category.name"
              required
              type="text"
              :class="{
                'form-control': true,
                'form-control-sm': true,
                'is-invalid': data.errors.includes(`name-${category.id}`),
              }"
            />
          </div>
          <div class="g-col-12 g-col-md-6 g-col-lg-4">
            <label :for="`order-${category.id}`" class="form-label">Order</label>
            <input
              :id="`order-${category.id}`"
              v-model="category.order"
              required
              type="number"
              :class="{
                'form-control': true,
                'form-control-sm': true,
                'is-invalid': data.errors.includes(`order-${category.id}`),
              }"
            />
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
    <div v-if="data.categories.length < 1" class="g-col-12 text-center">No categories.</div>
  </div>
</template>

<style lang="scss" scoped></style>
