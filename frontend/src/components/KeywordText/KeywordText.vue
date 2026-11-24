<template>
  <text class="keyword-text">
    <template v-for="(segment, index) in segments" :key="index">
      <text v-if="segment.keyword" class="keyword">{{ segment.text }}</text>
      <text v-else>{{ segment.text }}</text>
    </template>
  </text>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { splitKeywords } from '../../utils/keywords'

const props = defineProps<{
  text?: string | null
}>()

const segments = computed(() => splitKeywords(props.text))
</script>

<style lang="scss" scoped>
.keyword {
  font-style: italic;
  font-weight: 700;
}
</style>
