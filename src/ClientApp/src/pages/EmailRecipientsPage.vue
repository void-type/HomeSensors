<script lang="ts" setup>
import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router';
import type {
  EmailRecipientResponse,
  IItemSetOfIFailure,
} from '@/api/data-contracts';
import type { HttpResponse } from '@/api/http-client';
import type { ModalParameters } from '@/models/ModalParameters';
import { Collapse } from 'bootstrap';
import { nextTick, onBeforeUnmount, onMounted, reactive } from 'vue';
import { onBeforeRouteLeave, onBeforeRouteUpdate } from 'vue-router';
import ApiHelpers from '@/models/ApiHelpers';
import useAppStore from '@/stores/appStore';
import useMessageStore from '@/stores/messageStore';

const appStore = useAppStore();
const messageStore = useMessageStore();
const api = ApiHelpers.client;

const data = reactive({
  recipients: [] as Array<EmailRecipientResponse>,
  errors: [] as Array<string>,
  originalRecipients: new Map<number, string>(),
  hasDirtyRecipients: false,
});

function trackOriginalState(recipient: EmailRecipientResponse) {
  if (recipient.id !== undefined) {
    data.originalRecipients.set(
      recipient.id,
      JSON.stringify({
        email: recipient.email,
      }),
    );
  }
}

function isRecipientDirty(recipient: EmailRecipientResponse): boolean {
  if (recipient.id === 0) {
    return true;
  }
  if (recipient.id === undefined) {
    return false;
  }

  const original = data.originalRecipients.get(recipient.id);
  if (!original) {
    return false;
  }

  const current = JSON.stringify({
    email: recipient.email,
  });

  return original !== current;
}

function updateDirtyState() {
  data.hasDirtyRecipients = data.recipients.some(recipient => isRecipientDirty(recipient));
}

function handleBeforeUnload(event: BeforeUnloadEvent) {
  if (data.hasDirtyRecipients) {
    event.preventDefault();

    event.returnValue = '';
    return '';
  }
  return null;
}

function onRecipientInput() {
  updateDirtyState();
}

async function getRecipients() {
  try {
    const response = await api().emailRecipientsGetAll();
    data.recipients = response.data.slice().sort((a, b) => (a.email || '').localeCompare(b.email || ''));
    data.recipients.forEach(recipient => trackOriginalState(recipient));
    updateDirtyState();
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function newRecipient() {
  if (data.recipients.findIndex(x => (x.id || 0) < 1) > -1) {
    return;
  }

  const newRec = {
    id: 0,
    email: '',
  };

  data.recipients.unshift(newRec);
  updateDirtyState();

  await nextTick();
  const collapseEl = document.getElementById('collapse-0');
  if (collapseEl) {
    const collapse = new Collapse(collapseEl, { toggle: false });
    collapse.show();
  }
}

async function reallyDeleteRecipient(recipient: EmailRecipientResponse) {
  if (recipient.id === null || typeof recipient.id === 'undefined') {
    return;
  }

  try {
    const response = await api().emailRecipientsDelete({ id: recipient.id });
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }

    await getRecipients();
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function deleteRecipient(recipient: EmailRecipientResponse) {
  const parameters: ModalParameters = {
    title: 'Delete recipient',
    description: 'Do you really want to delete this email recipient?',
    okAction: () => reallyDeleteRecipient(recipient),
  };

  appStore.showModal(parameters);
}

async function saveRecipient(recipient: EmailRecipientResponse): Promise<boolean> {
  data.errors = [];

  const request = {
    id: recipient.id,
    email: recipient.email,
  };

  try {
    const response = await api().emailRecipientsSave(request);
    if (response.data.message) {
      messageStore.setSuccessMessage(response.data.message);
    }

    const isNewRecipient = recipient.id === 0;
    if (isNewRecipient) {
      const newItem = data.recipients.find(r => r.id === 0);
      if (newItem) {
        newItem.id = response.data.id;
        trackOriginalState(newItem);
        data.recipients.sort((a, b) => (a.email || '').localeCompare(b.email || ''));
      }
    } else {
      const existingIndex = data.recipients.findIndex(r => r.id === recipient.id);
      if (existingIndex >= 0) {
        data.recipients[existingIndex] = {
          ...data.recipients[existingIndex],
          ...request,
        };
        trackOriginalState(data.recipients[existingIndex]);
        data.recipients.sort((a, b) => (a.email || '').localeCompare(b.email || ''));
      }
    }

    updateDirtyState();
    return true;
  } catch (error) {
    const response = error as HttpResponse<unknown, unknown>;
    messageStore.setApiFailureMessages(response);

    const failures = (response.error as IItemSetOfIFailure).items || [];
    failures.forEach(x => data.errors.push(`${x.uiHandle}-${recipient.id}`));
    return false;
  }
}

async function saveAllDirty() {
  const dirtyItems = data.recipients.filter(item => isRecipientDirty(item));
  for (const item of dirtyItems) {
    if (!await saveRecipient(item)) {
      break;
    }
  }
}

function beforeRouteChange(
  to: RouteLocationNormalized,
  from: RouteLocationNormalized,
  next: NavigationGuardNext,
) {
  if (data.hasDirtyRecipients) {
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
  await getRecipients();
  window.addEventListener('beforeunload', handleBeforeUnload);
});

onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload);
});
</script>

<template>
  <div class="container-xxl">
    <h1 class="mt-3">
      Email Recipients
    </h1>
    <div class="mt-4">
      <button class="btn btn-primary" @click="newRecipient()">
        New
      </button>
      <button
        class="btn btn-secondary ms-2"
        :disabled="!data.hasDirtyRecipients"
        @click="saveAllDirty()"
      >
        Save All
      </button>
      <div id="recipientsAccordion" class="accordion mt-4">
        <div v-for="recipient in data.recipients" :key="recipient.id" class="accordion-item">
          <h2 :id="`heading-${recipient.id}`" class="accordion-header">
            <button
              class="accordion-button collapsed"
              type="button"
              data-bs-toggle="collapse"
              :data-bs-target="`#collapse-${recipient.id}`"
              :aria-expanded="false"
              :aria-controls="`collapse-${recipient.id}`"
            >
              <div class="d-flex align-items-center w-100">
                <span class="me-auto">
                  {{ recipient.email || "New recipient" }}
                  <span
                    v-if="isRecipientDirty(recipient)"
                    class="badge bg-warning text-dark ms-2"
                    role="button"
                    @click.stop="saveRecipient(recipient)"
                  >Unsaved</span>
                </span>
              </div>
            </button>
          </h2>
          <div
            :id="`collapse-${recipient.id}`"
            class="accordion-collapse collapse"
            :aria-labelledby="`heading-${recipient.id}`"
            data-bs-parent="#recipientsAccordion"
          >
            <div class="accordion-body">
              <div class="grid gap-sm">
                <div class="g-col-12 g-col-md-6 g-col-lg-4">
                  <label :for="`email-${recipient.id}`" class="form-label">Email</label>
                  <input
                    :id="`email-${recipient.id}`"
                    v-model="recipient.email"
                    required
                    type="email"
                    class="form-control form-control-sm"
                    :class="{
                      'is-invalid': data.errors.includes(`email-${recipient.id}`),
                    }"
                    @input="onRecipientInput"
                  >
                </div>
                <div class="g-col-12">
                  <div class="btn-toolbar">
                    <button class="btn btn-sm btn-primary me-2" @click="saveRecipient(recipient)">
                      Save
                    </button>
                    <button
                      v-if="recipient.id"
                      class="btn btn-sm btn-danger ms-auto"
                      @click="deleteRecipient(recipient)"
                    >
                      Delete
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div v-if="data.recipients.length < 1" class="text-center mt-4">
          No email recipients.
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped></style>
