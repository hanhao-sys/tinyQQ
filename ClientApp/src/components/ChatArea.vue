<template>
  <div class="chat-container">
    <!-- 头部 -->
    <div class="chat-header">
      <el-avatar :size="32" :icon="UserFilled" />
      <span class="chat-title">{{ friendName }} ({{ friendId }})</span>
    </div>

    <!-- 消息区 -->
    <div class="chat-messages" ref="msgContainer">
      <div v-for="msg in chat.messages" :key="msg.id" class="message-row"
        :class="{ 'is-self': msg.fromUserId === auth.user?.id }">
        <div class="message-bubble" :class="{ 'self': msg.fromUserId === auth.user?.id }">
          <div class="message-sender" v-if="msg.fromUserId !== auth.user?.id">
            {{ msg.sender }}
          </div>
          <div class="message-text">{{ msg.content }}</div>
          <div class="message-time">
            {{ formatTime(msg.messageTime) }}
            <el-icon v-if="msg.messageState === 0 && msg.fromUserId === auth.user?.id" :size="10"
              style="position:relative;top:2px"><WarningFilled /></el-icon>
          </div>
        </div>
      </div>
      <div v-if="chat.messages.length === 0" class="no-messages">
        暂无消息记录
      </div>
    </div>

    <!-- 输入区 -->
    <div class="chat-input">
      <el-input
        v-model="input"
        type="textarea"
        :rows="3"
        placeholder="输入消息... (Ctrl+Enter 发送)"
        resize="none"
        @keydown="handleKeydown"
      />
      <div class="chat-actions">
        <el-button type="primary" @click="sendMessage" :disabled="!input.trim()">发送</el-button>
        <el-button @click="loadHistory">消息记录</el-button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, nextTick, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { UserFilled, WarningFilled } from '@element-plus/icons-vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'
import { useChatStore } from '../stores/chat'

const props = defineProps({
  friendId: Number,
  friendName: String
})

const auth = useAuthStore()
const chat = useChatStore()
const input = ref('')
const msgContainer = ref(null)

// 加载最近消息
async function loadMessages() {
  try {
    const res = await api.get(`/message/${props.friendId}`, {
      params: { all: true, pageSize: 200 }
    })
    chat.setMessages(res.data.messages)
    markRead()
    scrollToBottom()
  } catch (e) { /* silent */ }
}

// 加载消息记录（重新加载）
async function loadHistory() {
  chat.setMessages([])
  await loadMessages()
  ElMessage.success('消息记录已刷新')
}

// 发送消息
async function sendMessage() {
  const text = input.value.trim()
  if (!text) return

  try {
    // 通过 SignalR 发送（实时推送）
    const conn = chat.signalr
    if (conn && conn.state === 'Connected') {
      await conn.invoke('SendMessage', props.friendId, text)
    } else {
      // 降级：直接 HTTP POST
      await api.post('/message/send', {
        toUserId: props.friendId,
        message: text
      })
      await loadMessages()
    }
    input.value = ''
    scrollToBottom()
  } catch (err) {
    ElMessage.error(err.response?.data?.error || '发送失败')
  }
}

// 标记已读
async function markRead() {
  try {
    await api.put(`/message/${props.friendId}/read`)
  } catch (e) { /* silent */ }
}

function handleKeydown(e) {
  if (e.ctrlKey && e.key === 'Enter') {
    sendMessage()
  }
}

function formatTime(time) {
  return new Date(time).toLocaleString('zh-CN', {
    month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit'
  })
}

function scrollToBottom() {
  nextTick(() => {
    if (msgContainer.value) {
      msgContainer.value.scrollTop = msgContainer.value.scrollHeight
    }
  })
}

watch(() => chat.messages.length, () => {
  scrollToBottom()
})

// 初始化
onMounted(async () => {
  await loadMessages()
})
</script>

<style scoped>
.chat-container { display: flex; flex-direction: column; height: 100%; }
.chat-header {
  display: flex; align-items: center; padding: 12px 16px;
  background: #fff; border-bottom: 1px solid #e4e7ed;
}
.chat-title { margin-left: 10px; font-size: 15px; font-weight: 600; }
.chat-messages {
  flex: 1; overflow-y: auto; padding: 16px; display: flex; flex-direction: column; gap: 8px;
}
.message-row { display: flex; }
.message-row.is-self { justify-content: flex-end; }
.message-bubble {
  max-width: 65%; padding: 10px 14px; border-radius: 12px; background: #fff; box-shadow: 0 1px 3px rgba(0,0,0,0.1);
}
.message-bubble.self { background: #95ec69; }
.message-sender { font-size: 12px; color: #409eff; margin-bottom: 4px; }
.message-text { font-size: 14px; word-break: break-word; }
.message-time { font-size: 11px; color: #999; text-align: right; margin-top: 4px; }
.no-messages { text-align: center; color: #ccc; margin-top: 60px; }
.chat-input { padding: 12px 16px; background: #fff; border-top: 1px solid #e4e7ed; }
.chat-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 8px; }
</style>
