import { ref } from 'vue'
import { defineStore } from 'pinia'

export const useChatStore = defineStore('chat', () => {
  const activeFriendId = ref(null)
  const activeFriendName = ref('')
  const messages = ref([])
  const unreadCount = ref(0)
  const signalr = ref(null)

  function setActiveFriend(id, name) {
    activeFriendId.value = id
    activeFriendName.value = name
  }

  function addMessage(msg) {
    messages.value.push(msg)
  }

  function setMessages(msgs) {
    messages.value = msgs
  }

  function setUnread(count) {
    unreadCount.value = count
  }

  function setSignalr(conn) {
    signalr.value = conn
  }

  return {
    activeFriendId, activeFriendName, messages, unreadCount, signalr,
    setActiveFriend, addMessage, setMessages, setUnread, setSignalr
  }
})
