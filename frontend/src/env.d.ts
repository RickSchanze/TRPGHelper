/// <reference types="@dcloudio/types" />

interface ImportMetaEnv {
  /** 后端地址，默认 http://localhost:5080 */
  readonly VITE_API_BASE?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

declare module '*.vue' {
  import type { DefineComponent } from 'vue'
  const component: DefineComponent<Record<string, never>, Record<string, never>, unknown>
  export default component
}
