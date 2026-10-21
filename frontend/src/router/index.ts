import { createRouter, createWebHistory } from 'vue-router'
import MissionsPlaceholder from '../features/missions/MissionsPlaceholder.vue'

export const router = createRouter({
  history: createWebHistory(),
  routes: [{ path: '/', name: 'missions', component: MissionsPlaceholder }],
})
