<template>
  <div class="admin-container">
    <!-- 头部 -->
    <div class="admin-header">
      <h2>tinyQQ 管理后台</h2>
      <div style="display:flex;gap:8px;align-items:center">
        <el-tag>{{ auth.user?.nickName }}</el-tag>
        <el-button @click="handleLogout" size="small">退出</el-button>
      </div>
    </div>

    <!-- 标签页 -->
    <el-tabs v-model="activeTab" type="border-card">
      <!-- 用户管理 -->
      <el-tab-pane label="用户管理" name="users">
        <div class="search-row">
          <el-input v-model="userSearch.id" placeholder="QQ号" size="small" style="width:120px;margin-right:8px" clearable />
          <el-input v-model="userSearch.nickName" placeholder="昵称" size="small" style="width:140px;margin-right:8px" clearable />
          <el-select v-model="userSearch.valid" placeholder="状态" size="small" style="width:100px;margin-right:8px" clearable>
            <el-option value="1" label="正常" /><el-option value="0" label="已封禁" />
          </el-select>
          <el-button type="primary" size="small" @click="searchUsers">搜索</el-button>
          <div style="margin-left:auto;display:flex;gap:8px">
            <el-button type="danger" size="small" @click="banUsers" :disabled="selectedUsers.length === 0">封禁</el-button>
            <el-button type="success" size="small" @click="unbanUsers" :disabled="selectedUsers.length === 0">解封</el-button>
            <el-button type="danger" size="small" @click="deleteUsers" :disabled="selectedUsers.length === 0">删除</el-button>
          </div>
        </div>

        <el-table
          :data="users"
          @selection-change="val => selectedUsers = val.map(x => x.id)"
          border stripe max-height="500"
          style="margin-top:12px"
        >
          <el-table-column type="selection" width="40" />
          <el-table-column prop="id" label="QQ号" width="70" sortable />
          <el-table-column prop="nickName" label="昵称" width="120" />
          <el-table-column prop="sex" label="性别" width="55" />
          <el-table-column prop="age" label="年龄" width="55" />
          <el-table-column prop="name" label="真实姓名" width="100" />
          <el-table-column prop="flag" label="在线" width="55">
            <template #default="{ row }">
              <el-tag :type="row.flag === 1 ? 'success' : 'info'" size="small">
                {{ row.flag === 1 ? '在线' : '离线' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="valid" label="状态" width="65">
            <template #default="{ row }">
              <el-tag :type="row.valid === 1 ? '' : 'danger'" size="small">
                {{ row.valid === 1 ? '正常' : '已封禁' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="sign" label="签名" min-width="120" show-overflow-tooltip />
        </el-table>
      </el-tab-pane>

      <!-- 消息管理 -->
      <el-tab-pane label="消息管理" name="messages">
        <div class="search-row">
          <el-input v-model="msgSearch.fromUserId" placeholder="发送者ID" size="small" style="width:100px;margin-right:8px" clearable />
          <el-input v-model="msgSearch.sender" placeholder="发送者昵称" size="small" style="width:120px;margin-right:8px" clearable />
          <el-input v-model="msgSearch.toUserId" placeholder="接收者ID" size="small" style="width:100px;margin-right:8px" clearable />
          <el-input v-model="msgSearch.receiver" placeholder="接收者昵称" size="small" style="width:120px;margin-right:8px" clearable />
          <el-input v-model="msgSearch.message" placeholder="消息关键字" size="small" style="width:140px;margin-right:8px" clearable />
          <el-button type="primary" size="small" @click="searchMessages">搜索</el-button>
          <div style="margin-left:auto">
            <el-button type="danger" size="small" @click="deleteMessages" :disabled="selectedMessages.length === 0">
              删除选中({{ selectedMessages.length }})
            </el-button>
          </div>
        </div>

        <el-table
          :data="messages"
          @selection-change="val => selectedMessages = val.map(x => x.id)"
          border stripe max-height="500"
          style="margin-top:12px"
        >
          <el-table-column type="selection" width="40" />
          <el-table-column prop="id" label="ID" width="60" sortable />
          <el-table-column prop="fromUserID" label="发送者ID" width="80" />
          <el-table-column prop="sender" label="发送者" width="100" />
          <el-table-column prop="toUserID" label="接收者ID" width="80" />
          <el-table-column prop="receiver" label="接收者" width="100" />
          <el-table-column prop="content" label="内容" min-width="200" show-overflow-tooltip>
            <template #default="{ row }">
              <span v-if="row.messageTypeID === 2" style="color:#e6a23c">[好友请求]</span>
              <span v-else>{{ row.content }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="messageTime" label="时间" width="155" sortable>
            <template #default="{ row }">
              {{ new Date(row.messageTime).toLocaleString('zh-CN') }}
            </template>
          </el-table-column>
        </el-table>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth = useAuthStore()
const activeTab = ref('users')

// ===== 用户管理 =====
const users = ref([])
const selectedUsers = ref([])
const userSearch = reactive({ id: '', nickName: '', valid: '' })

async function searchUsers() {
  try {
    const res = await api.get('/admin/users', { params: userSearch })
    users.value = res.data
  } catch (e) { ElMessage.error('查询失败') }
}

async function banUsers() {
  try {
    await ElMessageBox.confirm('确定要封禁选中的账号吗？', '提示', { type: 'warning' })
    await api.put('/admin/users/ban', { ids: selectedUsers.value })
    ElMessage.success('已封禁')
    await searchUsers()
  } catch (e) { if (e !== 'cancel') ElMessage.error('操作失败') }
}

async function unbanUsers() {
  try {
    await ElMessageBox.confirm('确定要解封选中的账号吗？', '提示', { type: 'warning' })
    await api.put('/admin/users/unban', { ids: selectedUsers.value })
    ElMessage.success('已解封')
    await searchUsers()
  } catch (e) { if (e !== 'cancel') ElMessage.error('操作失败') }
}

async function deleteUsers() {
  try {
    await ElMessageBox.confirm('删除后不可恢复，确定要删除选中的用户吗？', '提示', {
      type: 'warning', confirmButtonText: '确定删除'
    })
    await api.post('/admin/users/delete', { ids: selectedUsers.value })
    ElMessage.success('已删除')
    await searchUsers()
  } catch (e) { if (e !== 'cancel') ElMessage.error('操作失败') }
}

// ===== 消息管理 =====
const messages = ref([])
const selectedMessages = ref([])
const msgSearch = reactive({
  fromUserId: '', sender: '', toUserId: '', receiver: '', message: ''
})

async function searchMessages() {
  try {
    const res = await api.get('/admin/messages', { params: msgSearch })
    messages.value = res.data
  } catch (e) { ElMessage.error('查询失败') }
}

async function deleteMessages() {
  try {
    await ElMessageBox.confirm('删除后不可恢复，确定要删除选中的消息吗？', '提示', {
      type: 'warning', confirmButtonText: '确定删除'
    })
    await api.post('/admin/messages/delete', { ids: selectedMessages.value })
    ElMessage.success('已删除')
    await searchMessages()
  } catch (e) { if (e !== 'cancel') ElMessage.error('操作失败') }
}

async function handleLogout() {
  auth.logout()
  router.push('/login')
}

onMounted(() => {
  searchUsers()
  searchMessages()
})
</script>

<style scoped>
.admin-container { height: 100vh; display: flex; flex-direction: column; }
.admin-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 12px 20px; background: #409eff; color: #fff;
}
.admin-header h2 { font-size: 18px; }
.search-row { display: flex; align-items: center; padding: 12px 0; flex-wrap: wrap; gap: 4px; }
</style>
