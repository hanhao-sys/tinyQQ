import * as signalR from '@microsoft/signalr'
import { useAuthStore } from '../stores/auth'
import { useChatStore } from '../stores/chat'

let connection = null

export async function connectSignalR() {
  const auth = useAuthStore()
  const chat = useChatStore()

  if (connection && connection.state === signalR.HubConnectionState.Connected) {
    return connection
  }

  connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/chat', {
      accessTokenFactory: () => auth.token
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build()

  // 接收消息
  connection.on('ReceiveMessage', (msg) => {
    if (chat.activeFriendId === msg.fromUserId || chat.activeFriendId === msg.toUserId) {
      chat.addMessage(msg)
    }
    // 来自其他人的消息更新未读数
    if (chat.activeFriendId !== msg.fromUserId) {
      chat.setUnread(chat.unreadCount + 1)
    }
  })

  // 消息已发送确认
  connection.on('MessageSent', (msg) => {
    chat.addMessage({
      ...msg,
      fromUserId: auth.user?.id,
      sender: auth.user?.nickName,
      messageTypeId: 1
    })
  })

  // 好友请求
  connection.on('FriendRequest', (req) => {
    if (window.__friendRequestCallback) {
      window.__friendRequestCallback(req)
    }
  })

  // 未读消息数更新
  connection.on('UnreadUpdate', (count) => {
    chat.setUnread(count)
  })

  // 好友上下线
  connection.on('FriendStatusChanged', (status) => {
    if (window.__friendStatusCallback) {
      window.__friendStatusCallback(status)
    }
  })

  await connection.start()
  chat.setSignalr(connection)
  console.log('SignalR connected')
  return connection
}

export function getConnection() {
  return connection
}

export async function disconnectSignalR() {
  if (connection) {
    await connection.stop()
    connection = null
  }
}
