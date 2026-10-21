import { createRouter, createWebHistory } from 'vue-router'
import MissionsListView from '../features/missions/MissionsListView.vue'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/missions' },
    { path: '/missions', name: 'missions', component: MissionsListView, meta: { title: 'Missions' } },
    {
      path: '/missions/new',
      name: 'mission-new',
      component: () => import('../features/missions/MissionNewView.vue'),
      meta: { title: 'New mission' },
    },
    {
      path: '/missions/:missionId',
      name: 'mission-detail',
      component: () => import('../features/missions/MissionDetailView.vue'),
      props: true,
      meta: { title: 'Mission' },
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: () => import('../features/NotFoundView.vue'),
      meta: { title: 'Page not found' },
    },
  ],
})

router.afterEach((to) => {
  const title = typeof to.meta.title === 'string' ? to.meta.title : null
  document.title = title ? `${title} · ITS Middleware` : 'ITS Middleware'
})
