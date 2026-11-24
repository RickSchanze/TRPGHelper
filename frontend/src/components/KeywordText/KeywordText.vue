<template>
  <text class="keyword-text">
    <template v-for="(segment, index) in segments" :key="index">
      <text v-if="!segment.keyword">{{ segment.text }}</text>
      <text
        v-else-if="keywordState.ready && hasKeyword(segment.text)"
        class="keyword"
        @tap="onKeywordTap(segment.text)"
      >{{ segment.text }}</text>
      <text v-else-if="keywordState.ready">{{ '[' + segment.text + ']' }}</text>
      <text v-else>{{ segment.text }}</text>
    </template>
  </text>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { splitKeywords } from '../../utils/keywords'
import { ensureKeywords, findKeyword, keywordState, openKeyword } from '../../stores/keywords'

const props = defineProps<{
  text?: string | null
}>()

const segments = computed(() => splitKeywords(props.text))

onMounted(() => {
  void ensureKeywords()
})

/** 只有词条库里存在的关键词才加下划线、可点击；没有的原样显示 [关键词]。 */
function hasKeyword(key: string): boolean {
  return Boolean(findKeyword(key))
}

function onKeywordTap(key: string): void {
  if (!findKeyword(key)) {
    return
  }

  openKeyword(key)
}
</script>

<style lang="scss" scoped>
.keyword {
  font-style: italic;
  font-weight: 700;
  text-decoration: underline;
}

.keyword:active {
  opacity: 0.6;
}
</style>
