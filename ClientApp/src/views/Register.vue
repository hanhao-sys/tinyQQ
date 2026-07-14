<template>
  <div class="register-container">
    <div class="register-card">
      <h2 class="title">注册 tinyQQ 账号</h2>
      <el-form :model="form" :rules="rules" ref="formRef" label-width="80px">
        <el-form-item label="昵称" prop="nickName">
          <el-input v-model="form.nickName" placeholder="最多20个字" maxlength="20" />
        </el-form-item>
        <el-form-item label="性别" prop="sex">
          <el-radio-group v-model="form.sex">
            <el-radio value="男">男</el-radio>
            <el-radio value="女">女</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="年龄" prop="age">
          <el-input-number v-model="form.age" :min="0" :max="200" />
        </el-form-item>
        <el-form-item label="真实姓名">
          <el-input v-model="form.name" placeholder="选填" />
        </el-form-item>
        <el-form-item label="星座">
          <el-select v-model="form.star" placeholder="选填" clearable>
            <el-option v-for="s in stars" :key="s" :value="s" :label="s" />
          </el-select>
        </el-form-item>
        <el-form-item label="血型">
          <el-select v-model="form.bloodType" placeholder="选填" clearable>
            <el-option v-for="b in bloodTypes" :key="b" :value="b" :label="b" />
          </el-select>
        </el-form-item>
        <el-form-item label="密码" prop="password">
          <el-input v-model="form.password" type="password" show-password />
        </el-form-item>
        <el-form-item label="确认密码" prop="password2">
          <el-input v-model="form.password2" type="password" show-password />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="loading" @click="handleRegister" style="width:100%">
            注 册
          </el-button>
        </el-form-item>
      </el-form>
      <div class="footer">
        <el-link type="primary" @click="$router.push('/login')">已有账号？去登录</el-link>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import api from '../services/api'

const router = useRouter()
const formRef = ref(null)
const loading = ref(false)

const stars = ['白羊座','金牛座','双子座','巨蟹座','狮子座','处女座','天秤座','天蝎座','射手座','摩羯座','水瓶座','双鱼座']
const bloodTypes = ['A型','B型','AB型','O型']

const form = reactive({
  nickName: '', sex: '', age: 20, name: '', star: '', bloodType: '',
  password: '', password2: ''
})

const validatePass2 = (rule, value, callback) => {
  if (value !== form.password) {
    callback(new Error('两次输入的密码不一致'))
  } else {
    callback()
  }
}

const rules = {
  nickName: [{ required: true, message: '请输入昵称', trigger: 'blur' }],
  sex: [{ required: true, message: '请选择性别', trigger: 'change' }],
  age: [{ required: true, message: '请输入年龄', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }],
  password2: [
    { required: true, message: '请确认密码', trigger: 'blur' },
    { validator: validatePass2, trigger: 'blur' }
  ]
}

async function handleRegister() {
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  loading.value = true
  try {
    const res = await api.post('/account/register', {
      nickName: form.nickName,
      password: form.password,
      sex: form.sex,
      age: form.age,
      name: form.name || null,
      star: form.star || null,
      bloodType: form.bloodType || null
    })
    ElMessage.success(res.data.message)
    router.push('/login')
  } catch (err) {
    ElMessage.error(err.response?.data?.error || '注册失败')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.register-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}
.register-card {
  width: 460px;
  padding: 30px 40px;
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 20px 60px rgba(0,0,0,0.2);
}
.title { text-align: center; color: #409eff; margin-bottom: 24px; }
.footer { text-align: center; margin-top: 10px; }
</style>
