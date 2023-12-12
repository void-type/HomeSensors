<script lang="ts" setup>
import { computed, onMounted, reactive } from 'vue';
import * as signalR from '@microsoft/signalr';
import ApiHelpers from '@/models/ApiHelpers';
import type { ClientStatus } from '@/api/data-contracts';
import useMessageStore from '@/stores/messageStore';
import type { HttpResponse } from '@/api/http-client';
import DateHelpers from '@/models/DateHelpers';
import { parseISO } from 'date-fns';

const data = reactive({
  topics: 'rtl_433/#\nzigbee2mqtt/#',
  feed: [] as Array<string>,
  status: { topics: [] as Array<string>, exists: false, isConnected: false } as ClientStatus,
});

const messageStore = useMessageStore();
const api = ApiHelpers.client;

let connection: signalR.HubConnection | null = null;

async function connectToHub() {
  const startTimeMilliseconds = Date.now();

  async function startConnection() {
    if (connection !== null) {
      try {
        await connection.start();
      } catch {
        // If we fail to start the connection, retry after delay.
        const elapsedMilliseconds = Date.now() - startTimeMilliseconds;
        const delay = ApiHelpers.getRetryMilliseconds(elapsedMilliseconds);
        if (delay !== null) {
          setTimeout(startConnection, delay);
        }
      }
    }
  }

  if (connection === null) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl('/hub/temperatures')
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) =>
          ApiHelpers.getRetryMilliseconds(retryContext.elapsedMilliseconds),
      })
      .build();

    connection.on('newDiscoveryMessage', (message) => {
      const formattedMessage = `${DateHelpers.dateTimeForApi(parseISO(message.time))}: ${
        message.topic
      }\n${message.payload}\n`;
      data.feed.unshift(formattedMessage);
    });
  }

  startConnection();
}

const feed = computed(() => data.feed.join('\n'));

async function onRefresh() {
  data.status = (await api().temperaturesMqttFeedDiscoveryStatusList())?.data;
}

async function onStart() {
  try {
    const response = await api().temperaturesMqttFeedDiscoverySetupCreate({
      topics: data.topics.split(/\r?\n/g),
    });

    data.status = response?.data;

    // Recheck status to see if it connected.
    setTimeout(() => onRefresh(), 500);
  } catch (error) {
    messageStore.setApiFailureMessages(error as HttpResponse<unknown, unknown>);
  }
}

async function onEnd() {
  data.status = (await api().temperaturesMqttFeedDiscoveryTeardownCreate())?.data;
}

async function onClear() {
  data.feed.length = 0;
}

onMounted(async () => {
  await connectToHub();
  await onRefresh();

  const { topics } = data.status;

  if (topics) {
    data.topics = topics.join('\n');
  }
});
</script>

<template>
  <div class="container-xxl">
    <h1 class="mt-4 mb-4">Discovery</h1>
    <p>See MQTT messages from the specified topics. New messages appear on top.</p>
    <div class="btn-toolbar mt-3">
      <button class="btn btn-primary me-2" @click.prevent.stop="onStart">Start</button>
      <button class="btn btn-outline-light me-2" @click.prevent.stop="onEnd">End</button>
      <button class="btn btn-outline-light me-2" @click.prevent.stop="onClear">Clear</button>
      <button class="btn btn-outline-light me-2" @click.prevent.stop="onRefresh">
        Refresh status
      </button>
    </div>
    <div class="mt-3">
      <label for="status" class="form-label">Status</label>
      <input
        id="status"
        class="form-control"
        type="text"
        disabled
        :value="data.status.isConnected ? 'Running' : 'Not running'"
      />
    </div>
    <div class="mt-3">
      <label for="topics" class="form-label">Topics (one per line)</label>
      <textarea id="topics" v-model="data.topics" rows="3" class="form-control" />
    </div>
    <div class="mt-3">
      <label for="feed" class="form-label">
        Feed messages (Time | Topic | Model/Id/Channel | Temp | Humidity)
      </label>
      <textarea id="feed" v-model="feed" rows="30" class="form-control" readonly />
    </div>
  </div>
</template>

<style lang="scss" scoped></style>
