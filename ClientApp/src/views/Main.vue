<template>
  <div class="main-container">
    <!-- 左侧：好友列表 -->
    <div class="sidebar">
      <div class="user-info">
        <el-avatar :size="40" :icon="UserFilled" />
        <div class="user-text">
          <div class="user-name">{{ auth.user?.nickName || '用户' }} ({{ auth.user?.id }})</div>
          <div class="user-sign" v-if="auth.user?.sign">{{ auth.user?.sign }}</div>
        </div>
        <el-dropdown trigger="click">
          <el-icon :size="20" style="cursor:pointer"><MoreFilled /></el-icon>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item @click="showProfile = true">编辑资料</el-dropdown-item>
              <el-dropdown-item @click="handleLogout">退出登录</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>

      <!-- 搜索栏 -->
      <div class="search-bar">
        <el-input v-model="searchKeyword" placeholder="搜索好友/用户..." size="small" clearable
          @keyup.enter="handleSearch">
          <template #suffix><el-icon @click="handleSearch"><Search /></el-icon></template>
        </el-input>
      </div>

      <!-- 好友列表 -->
      <div class="friend-list">
        <div class="section-title">我的好友 ({{ friends.length }})</div>
        <div
          v-for="f in friends"
          :key="f.id"
          class="friend-item"
          :class="{ active: chat.activeFriendId === f.id }"
          @click="openChat(f)"
        >
          <el-badge :is-dot="f.flag === 1" type="success" :offset="[6, 32]">
            <el-avatar :size="36" :src="headUrl(f.headID)" />
          </el-badge>
          <div class="friend-info">
            <div class="friend-name">{{ f.nickName }}</div>
            <div class="friend-sign">{{ f.sign || '' }}</div>
          </div>
          <span class="online-dot" :class="{ online: f.flag === 1 }"></span>
        </div>
        <el-empty v-if="friends.length === 0" description="暂无好友" />
      </div>

      <!-- 操作按钮 -->
      <div class="sidebar-actions">
        <el-button type="primary" size="small" @click="showSearch = true" style="width:100%">
          <el-icon><Plus /></el-icon> 添加好友
        </el-button>
        <el-button size="small" @click="refreshFriends" style="width:100%">
          <el-icon><Refresh /></el-icon> 刷新列表
        </el-button>
      </div>

      <!-- 未读消息徽标 -->
      <div class="friend-request-badge" @click="showRequests = true" v-if="requestCount > 0">
        <el-badge :value="requestCount" :max="99">
          <el-button size="small">📩 好友请求</el-button>
        </el-badge>
      </div>
    </div>

    <!-- 右侧：聊天区 -->
    <div class="chat-area">
      <template v-if="chat.activeFriendId">
        <ChatArea :friend-id="chat.activeFriendId" :friend-name="chat.activeFriendName" />
      </template>
      <template v-else>
        <div class="no-chat">
          <el-icon :size="80" color="#ccc"><ChatDotRound /></el-icon>
          <p>选择一个好友开始聊天</p>
        </div>
      </template>
    </div>

    <!-- 搜索用户弹窗 -->
    <el-dialog v-model="showSearch" title="查找好友" width="500px">
      <div class="search-filters">
        <el-input v-model="searchKeyword" placeholder="输入QQ号或昵称" style="width:200px;margin-right:10px" />
        <el-select v-model="searchSex" placeholder="性别" clearable style="width:100px;margin-right:10px">
          <el-option value="男" label="男" /><el-option value="女" label="女" />
        </el-select>
        <el-button type="primary" @click="handleSearch">搜索</el-button>
      </div>
      <el-table :data="searchResults" style="margin-top:16px" max-height="300">
        <el-table-column prop="id" label="QQ号" width="80" />
        <el-table-column prop="nickName" label="昵称" />
        <el-table-column prop="sex" label="性别" width="60" />
        <el-table-column prop="age" label="年龄" width="60" />
        <el-table-column label="操作" width="100">
          <template #default="{ row }">
            <el-button type="primary" size="small" text @click="addFriend(row.id)">加好友</el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-empty v-if="searched && searchResults.length === 0" description="未找到用户" />
    </el-dialog>

    <!-- 好友请求弹窗 -->
    <el-dialog v-model="showRequests" title="好友请求" width="400px">
      <div v-if="friendRequests.length === 0">
        <el-empty description="暂无好友请求" />
      </div>
      <div v-for="req in friendRequests" :key="req.messageId" class="request-item">
        <el-avatar :size="32" :icon="UserFilled" />
        <span>{{ req.fromNickName }} ({{ req.fromUserId }}) 请求加您为好友</span>
        <div style="margin-left:auto">
          <el-button type="primary" size="small" @click="approveRequest(req)">同意</el-button>
          <el-button size="small" @click="rejectRequest(req)">拒绝</el-button>
        </div>
      </div>
    </el-dialog>

    <!-- 编辑资料弹窗 -->
    <el-dialog v-model="showProfile" title="编辑个人信息" width="420px">
      <el-form :model="profile" label-width="80px">
        <el-form-item label="昵称"><el-input v-model="profile.nickName" /></el-form-item>
        <el-form-item label="性别">
          <el-radio-group v-model="profile.sex"><el-radio value="男">男</el-radio><el-radio value="女">女</el-radio></el-radio-group>
        </el-form-item>
        <el-form-item label="年龄"><el-input-number v-model="profile.age" :min="0" /></el-form-item>
        <el-form-item label="签名"><el-input v-model="profile.sign" /></el-form-item>
        <el-form-item label="新密码"><el-input v-model="profile.newPassword" type="password" placeholder="留空不修改" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showProfile = false">取消</el-button>
        <el-button type="primary" @click="saveProfile">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { UserFilled, MoreFilled, Search, Plus, Refresh, ChatDotRound } from '@element-plus/icons-vue'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'
import { useChatStore } from '../stores/chat'
import { connectSignalR, disconnectSignalR } from '../services/signalr'
import ChatArea from '../components/ChatArea.vue'

const router = useRouter()
const auth = useAuthStore()
const chat = useChatStore()

// ===== 好友列表 =====
const friends = ref([])
const requestCount = ref(0)

async function refreshFriends() {
  try {
    const res = await api.get('/friend')
    friends.value = res.data
  } catch (e) { /* silent */ }
}

async function refreshRequestCount() {
  try {
    const res = await api.get('/friend/requests')
    requestCount.value = res.data.length
  } catch (e) { /* silent */ }
}

// ===== 搜索用户 =====
const showSearch = ref(false)
const searched = ref(false)
const searchKeyword = ref('')
const searchSex = ref('')
const searchResults = ref([])

async function handleSearch() {
  if (!searchKeyword.value.trim() && !searchSex.value) return
  searched.value = true
  try {
    const res = await api.get('/friend/search', {
      params: { keyword: searchKeyword.value, sex: searchSex.value }
    })
    searchResults.value = res.data
  } catch (e) {
    ElMessage.error('搜索失败')
  }
}

async function addFriend(friendId) {
  try {
    await api.post(`/friend/request/${friendId}`)
    ElMessage.success('已发出好友请求')
  } catch (err) {
    ElMessage.error(err.response?.data?.error || '添加失败')
  }
}

// ===== 好友请求 =====
const showRequests = ref(false)
const friendRequests = ref([])

async function loadRequests() {
  try {
    const res = await api.get('/friend/requests')
    friendRequests.value = res.data
    requestCount.value = res.data.length
  } catch (e) { /* silent */ }
}

async function approveRequest(req) {
  try {
    await api.post(`/friend/approve/${req.messageId}`)
    ElMessage.success('已添加为好友')
    refreshFriends()
    loadRequests()
  } catch (err) {
    ElMessage.error(err.response?.data?.error || '操作失败')
  }
}

async function rejectRequest(req) {
  try {
    await api.post(`/friend/reject/${req.messageId}`)
    ElMessage.info('已拒绝')
    loadRequests()
  } catch (err) {
    ElMessage.error(err.response?.data?.error || '操作失败')
  }
}

// ===== 聊天 =====
function openChat(friend) {
  chat.setActiveFriend(friend.id, friend.nickName)
}

// ===== 个人资料 =====
const showProfile = ref(false)
const profile = reactive({
  nickName: '', sex: '', age: 0, sign: '', newPassword: ''
})

// 初始化 profiles
onMounted(async () => {
  profile.nickName = auth.user?.nickName || ''
  profile.sex = auth.user?.sex || ''
  profile.age = auth.user?.age || 0
  profile.sign = auth.user?.sign || ''
})

async function saveProfile() {
  try {
    const res = await api.put('/account/profile', profile)
    auth.user = res.data.user
    localStorage.setItem('user', JSON.stringify(res.data.user))
    ElMessage.success('保存成功')
    showProfile.value = false
  } catch (err) {
    ElMessage.error(err.response?.data?.error || '保存失败')
  }
}

async function handleLogout() {
  try {
    await api.post('/account/logout')
  } catch (e) { /* silent */ }
  await disconnectSignalR()
  auth.logout()
  router.push('/login')
}

// SignalR 回调
window.__friendRequestCallback = (req) => {
  ElMessage.info(`${req.fromNickName} 请求加您为好友`)
  refreshRequestCount()
}
window.__friendStatusCallback = (status) => {
  const f = friends.value.find(x => x.id === status.userId)
  if (f) f.flag = status.flag
}

// ===== 生命周期 =====
onMounted(async () => {
  await connectSignalR()
  await refreshFriends()
  await refreshRequestCount()
})
</script>

<style scoped>
.main-container { display: flex; height: 100vh; }
.sidebar {
  width: 280px; min-width: 280px; background: #eef1f6;
  display: flex; flex-direction: column; border-right: 1px solid #ddd;
}
.user-info {
  display: flex; align-items: center; padding: 14px 12px;
  background: #409eff; color: #fff;
}
.user-text { flex: 1; margin-left: 10px; overflow: hidden; }
.user-name { font-size: 14px; font-weight: 600; white-space: nowrap; }
.user-sign { font-size: 12px; opacity: 0.8; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.search-bar { padding: 10px 12px; }
.friend-list { flex: 1; overflow-y: auto; padding: 4px 0; }
.section-title { font-size: 12px; color: #999; padding: 8px 12px 4px; }
.friend-item {
  display: flex; align-items: center; padding: 10px 12px; cursor: pointer; transition: background 0.2s;
}
.friend-item:hover { background: #dce6f1; }
.friend-item.active { background: #c4d9f0; }
.friend-info { flex: 1; margin-left: 10px; overflow: hidden; }
.friend-name { font-size: 14px; white-space: nowrap; }
.friend-sign { font-size: 12px; color: #999; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.online-dot { width: 8px; height: 8px; border-radius: 50%; background: #ccc; }
.online-dot.online { background: #67c23a; }
.sidebar-actions { padding: 10px 12px; display: flex; flex-direction: column; gap: 6px; }
.friend-request-badge { padding: 0 12px 10px; cursor: pointer; }
.chat-area { flex: 1; display: flex; flex-direction: column; background: #f5f5f5; }
.no-chat { flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: center; color: #ccc; }
.no-chat p { margin-top: 16px; font-size: 16px; }
.request-item {
  display: flex; align-items: center; padding: 10px 0; gap: 10px;
  border-bottom: 1px solid #ebeef5;
}
.search-filters { display: flex; align-items: center; }
</style>
