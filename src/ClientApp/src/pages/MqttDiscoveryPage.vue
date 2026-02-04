<script lang="ts" setup>
import type { PropType } from 'vue';
import type { MqttDiscoveryClientStatus } from '@/models/MqttDiscoveryClientStatus';
import * as signalR from '@microsoft/signalr';
import { parseISO } from 'date-fns';
import { computed, onMounted, reactive } from 'vue';
import ApiHelpers from '@/models/ApiHelpers';
import DateHelpers from '@/models/DateHelpers';
import { formatJSON, isNil } from '@/models/FormatHelpers';
import useAppStore from '@/stores/appStore';
import useMessageStore from '@/stores/messageStore';

const props = defineProps({
  topics: {
    type: Array as PropType<Array<string>>,
    required: false,
    default: () => [],
  },
});

const data = reactive({
  topics: '',
  feed: [] as Array<string>,
  status: {
    topics: [] as Array<string>,
    exists: false,
    isConnected: false,
  } as MqttDiscoveryClientStatus,
});

const appStore = useAppStore();
const messageStore = useMessageStore();

const { isFieldInError } = appStore;

let connection: signalR.HubConnection | null = null;

async function connectToHub() {
  const startTimeMilliseconds = Date.now();

  async function startConnection() {
    if (connection !== null) {
      try {
        await connection.start();
        await onRefresh();
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
        nextRetryDelayInMilliseconds: retryContext =>
          ApiHelpers.getRetryMilliseconds(retryContext.elapsedMilliseconds),
      })
      .build();

    connection.on('newDiscoveryMessage', (message) => {
      const formattedMessage = `${DateHelpers.dateTimeForApi(parseISO(message.time))}: ${
        message.topic
      }\n${formatJSON(message.payload)}\n`;
      data.feed.unshift(formattedMessage);
      data.feed = data.feed.slice(0, 2000);
    });

    connection.on('updateDiscoveryStatus', (status: MqttDiscoveryClientStatus) => {
      data.status = status;
      data.topics = status.topics?.join('\n') ?? data.topics;
    });

    connection.onreconnected(async () => {
      await onRefresh();
    });
  }

  startConnection();
}

const feed = computed(() => data.feed.join('\n'));

async function onRefresh() {
  if (connection === null || connection.state !== signalR.HubConnectionState.Connected) {
    return;
  }

  data.status = await connection.invoke<MqttDiscoveryClientStatus>('getDiscoveryStatus');
  data.topics = data.status.topics?.join('\n') ?? data.topics;
}

async function onStart() {
  if (connection === null || connection.state !== signalR.HubConnectionState.Connected) {
    return;
  }

  await onEnd();

  try {
    await connection.invoke('setupDiscovery', {
      topics: data.topics.split(/\r?\n/g).filter(x => !isNil(x)),
    });

    // Set querystring topic= for each topic
    const url = new URL(window.location.href);
    // clear existing topics
    url.searchParams.delete('topic');
    data.topics.split(/\r?\n/g).forEach((topic) => {
      url.searchParams.append('topic', topic);
    });
    window.history.replaceState({}, '', url.toString());
  } catch (error) {
    messageStore.setErrorMessage((error as Error).message);
  }
}

async function onEnd() {
  if (connection === null || connection.state !== signalR.HubConnectionState.Connected) {
    return;
  }

  await connection.invoke('teardownDiscovery');
}

async function onClear() {
  data.feed.length = 0;
}

onMounted(async () => {
  await connectToHub();
  await onRefresh();

  const statusTopics = data.status.topics?.join('\n');
  const propsTopics = props.topics.join('\n');

  if (propsTopics && propsTopics !== statusTopics) {
    // User requested new topics.
    data.topics = propsTopics;

    if (data.status.isConnected) {
      await onEnd();
    }

    await onStart();
  } else if (statusTopics) {
    // Server topics exist, update to match.
    data.topics = statusTopics;
  } else {
    data.topics = ['rtl_433/#', 'zigbee2mqtt/#'].join('\n');
  }
});
</script>

<template>
  <div class="container-xxl">
    <h1 class="mt-3">
      MQTT Discovery
    </h1>
    <p class="mt-4">
      See MQTT messages from the specified topics.
    </p>
    <ul>
      <li>+ is a single-level wildcard. Can be used anywhere in a topic to sub a level.</li>
      <li>
        # is a multi-level wildcard. Can only be used at the end of a topic preceded by a forward
        slash.
      </li>
    </ul>
    <div class="btn-toolbar mt-4">
      <button class="btn btn-primary me-2 mb-2" @click.prevent.stop="onStart">
        Start
      </button>
      <button class="btn btn-secondary me-2 mb-2" @click.prevent.stop="onEnd">
        End
      </button>
      <button class="btn btn-secondary me-2 mb-2" @click.prevent.stop="onClear">
        Clear
      </button>
      <button class="btn btn-secondary me-2 mb-2" @click.prevent.stop="onRefresh">
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
      >
    </div>
    <div class="mt-3">
      <label for="topics" class="form-label">Topics (one per line)</label>
      <textarea id="topics" v-model="data.topics" rows="3" class="form-control" />
    </div>
    <div class="mt-3">
      <label for="feed" class="form-label"> Feed messages (new messages appear on top)</label>
      <textarea
        id="feed"
        v-model="feed"
        rows="30"
        class="form-control" :class="{ 'is-invalid': isFieldInError('topics') }"
        readonly
      />
    </div>
  </div>
</template>

<style lang="scss" scoped></style>
