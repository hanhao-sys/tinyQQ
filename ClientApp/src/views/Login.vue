<template>
  <div class="login-container">
    <div class="login-card">
      <h2 class="title">tinyQQ</h2>
      <p class="subtitle">Web 端登录</p>
      <el-form :model="form" :rules="rules" ref="formRef" label-width="0">
        <el-form-item prop="id">
          <el-input v-model="form.id" placeholder="QQ 号码" size="large" />
        </el-form-item>
        <el-form-item prop="password">
          <el-input v-model="form.password" type="password" placeholder="密码" size="large"
            @keyup.enter="handleLogin" show-password />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" size="large" :loading="loading" @click="handleLogin" style="width:100%">
            登 录
          </el-button>
        </el-form-item>
      </el-form>
      <div class="footer">
        <el-link type="primary" @click="$router.push('/register')">注册新账号</el-link>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth = useAuthStore()
const formRef = ref(null)
const loading = ref(false)

const form = reactive({
  id: '',
  password: ''
})

const rules = {
  id: [{ required: true, message: '请输入QQ号码', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

async function handleLogin() {
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  loading.value = true
  try {
    const res = await api.post('/account/login', {
      id: parseInt(form.id),
      password: form.password
    })
    auth.setAuth(res.data.token, res.data.user)
    ElMessage.success('登录成功')
    if (res.data.user.id === 123) {
      router.push('/admin')
    } else {
      router.push('/')
    }
  } catch (err) {
    ElMessage.error(err.response?.data?.error || '登录失败')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-container {
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}
.login-card {
  width: 380px;
  padding: 40px;
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 20px 60px rgba(0,0,0,0.2);
}
.title {
  text-align: center;
  font-size: 28px;
  color: #409eff;
  margin-bottom: 4px;
}
.subtitle {
  text-align: center;
  color: #999;
  margin-bottom: 30px;
}
.footer {
  text-align: center;
  margin-top: 10px;
}
</style>
