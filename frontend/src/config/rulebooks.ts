/**
 * 规则书图标配置。
 *
 * 正式版替换方式（任选一种）：
 * 1. 直接把同名 PNG 覆盖到 src/static/rulebooks/ 下（dnd5e.png / coc7.png），代码不用动；
 * 2. 想换文件名或加新规则书，改下面的 icon 路径，并在后端 GameSystems 里加对应 code。
 *
 * 建议素材：256px 以上、透明背景的 PNG；深色底上的图标用白色剪影效果最好。
 *
 * 当前素材为开发期临时使用：
 * - dnd5e.png: "dice-twenty-faces-twenty" by Delapouite / game-icons.net (CC BY 3.0)
 * - coc7.png:  "giant-squid" by Delapouite / game-icons.net (CC BY 3.0)
 * 来源：https://game-icons.net
 */

export interface RulebookArtwork {
  /** 图标路径，放在 src/static 下，编译后会原样拷贝到小程序 / H5 的 static 目录 */
  icon: string
  /** 图标底色的主色，和图标一起决定卡片观感 */
  accent: string
}

export const rulebookArtwork: Record<string, RulebookArtwork> = {
  dnd5e: {
    icon: '/static/rulebooks/dnd5e.png',
    accent: '#5a1b1b',
  },
  coc7: {
    icon: '/static/rulebooks/coc7.png',
    accent: '#123f37',
  },
}

const fallbackArtwork: RulebookArtwork = {
  icon: '',
  accent: '#5b4636',
}

export function artworkFor(code: string): RulebookArtwork {
  return rulebookArtwork[code] ?? fallbackArtwork
}
