/**
 * 职业卡片的配图配置。
 *
 * 正式版替换：直接覆盖 src/static/classes/paladin.jpg（同名即可），
 * 或者改下面的 image 路径；想给别的职业加图，也在下面补一条。
 *
 * 当前素材：开发期临时使用的圣武士插图（用户提供）。
 */

export interface ClassArtwork {
  image: string
  accent: string
}

export const classArtwork: Record<string, ClassArtwork> = {
  paladin: {
    image: '/static/classes/paladin.jpg',
    accent: '#b8862f',
  },
  fighter: {
    image: '/static/classes/fighter.jpg',
    accent: '#3d5a8a',
  },
  monk: {
    image: '/static/classes/monk.jpg',
    accent: '#3f5a3a',
  },
}

const fallbackArtwork: ClassArtwork = {
  image: '',
  accent: '#5b4636',
}

export function classArtworkFor(code: string): ClassArtwork {
  return classArtwork[code] ?? fallbackArtwork
}
