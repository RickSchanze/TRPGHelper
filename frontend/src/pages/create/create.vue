<template>
  <view class="page wizard-page" :class="{ 'wizard-page-docked': flowActive }">
    <AppHeader current="create" />

    <view class="hero">
      <view class="hero-en">WHO ARE YOU</view>
      <view class="hero-zh">你是谁</view>
    </view>

    <!-- 前置条件：选择规则书。它不算车卡流程里的步骤。 -->
    <view v-if="!started" class="panel">
      <view class="panel-title">选择规则书</view>
      <view class="muted rulebook-hint">规则书决定属性模板和素材范围，选完才进入车卡流程，它本身不算一步。</view>

      <view class="system-grid">
        <view
          v-for="system in systems"
          :key="system.code"
          class="system-card"
          :class="{
            'system-selected': draft.systemCode === system.code,
            'system-locked': !system.available,
          }"
          @tap="pickSystem(system)"
        >
          <view class="system-icon-wrap" :style="{ backgroundColor: system.accent || DEFAULT_ACCENT }">
            <image
              v-if="system.iconUrl && !failedIcons[system.code]"
              class="system-icon"
              :src="system.iconUrl"
              mode="aspectFit"
              @error="markIconFailed(system.code)"
            />
            <text v-else class="system-icon-fallback">{{ system.name.slice(0, 1) }}</text>
          </view>

          <view class="system-info">
            <view class="system-name">
              <KeywordText :text="system.name" />
            </view>
            <view class="system-desc muted">
              <KeywordText :text="system.description" />
            </view>
          </view>

          <text v-if="!system.available" class="system-lock muted">即将开放</text>
          <view v-else-if="draft.systemCode === system.code" class="system-check">✓</view>
        </view>
      </view>

      <view class="rulebook-actions">
        <button
          class="btn"
          hover-class="btn-pressed"
          hover-stay-time="80"
          :disabled="!draft.systemCode || flowLoading"
          @tap="startFlow"
        >
          {{ flowLoading ? '正在加载流程…' : '开始车卡' }}
        </button>
      </view>
    </view>

    <!-- 车卡流程：步骤数量和顺序来自管理台配置 -->
    <template v-else>
      <view v-if="flowLoading" class="panel">
        <text class="muted">正在加载车卡流程…</text>
      </view>

      <view v-else-if="flowError" class="panel load-error" @tap="loadFlow">
        <text class="muted">流程加载失败，点这里重试</text>
      </view>

      <view v-else-if="!steps.length" class="panel">
        <view class="panel-title">这个规则书还没配置车卡流程</view>
        <text class="muted">去管理台的「车卡流程」里加一步（比如「选择职业」），再回来刷新。</text>
      </view>

      <template v-else>
        <view
          class="stepper-viewport"
          @touchstart="onStepperTouchStart"
          @touchmove="onStepperTouchMove"
          @touchend="onStepperTouchEnd"
          @touchcancel="onStepperTouchEnd"
        >
          <view
            class="stepper"
            :style="{
              transform: `translateX(-${stepperOffset}px)`,
              transition: stepperDragging ? 'none' : 'transform 0.25s ease',
            }"
          >
            <view
              v-for="(item, index) in steps"
              :key="item.id"
              class="step"
              :class="{
                'step-active': step === index + 1,
                'step-done': isStepDone(index),
                'step-clickable': isStepDone(index) || step > index + 1,
              }"
              @tap="jumpToStep(index)"
            >
              <view class="step-dot">{{ isStepDone(index) && step !== index + 1 ? '✓' : index + 1 }}</view>
              <KeywordText class="step-label" :text="item.title" />
            </view>
          </view>
        </view>

        <!-- 职业步骤：唯一已实现的面板 -->
        <view v-if="isClassStep" class="panel">
          <view class="panel-title">
            <KeywordText :text="currentStep?.title || '选择职业'" />
          </view>
          <view v-if="currentStep?.description" class="muted step-hint">
            <KeywordText :text="currentStep.description" />
          </view>

          <view v-if="classesLoading" class="muted">正在加载职业…</view>
          <view v-else-if="classesError" class="load-error" @tap="loadClasses">
            <text class="muted">职业加载失败，点这里重试</text>
          </view>

          <view v-else class="class-list">
            <view
              v-for="item in classes"
              :key="item.code"
              class="class-card"
              :class="{ 'class-selected': draft.classCode === item.code }"
              @tap="pickClass(item)"
            >
              <view class="class-cover-wrap">
                <image
                  v-if="coverFor(item) && !failedCovers[item.code]"
                  class="class-cover"
                  :src="coverFor(item)"
                  mode="aspectFit"
                  @error="failedCovers[item.code] = true"
                />
                <view
                  v-else
                  class="class-cover class-cover-fallback"
                  :style="{ backgroundColor: item.accent || DEFAULT_ACCENT }"
                >
                  <text>{{ item.name.slice(0, 1) }}</text>
                </view>
              </view>

              <view class="class-body">
                <view class="class-head">
                  <view class="class-name">
                    <KeywordText :text="item.name" />
                  </view>
                  <view class="class-detail" @tap.stop="openDetail(item)">详情 ›</view>
                </view>
                <view class="class-sub">
                  <text>生命骰 </text>
                  <KeywordText :text="item.hitDie" />
                  <text> · 主要属性 </text>
                  <KeywordText :text="item.primaryAbility" />
                </view>
                <view class="class-desc">
                  <KeywordText :text="item.description" />
                </view>
                <view class="feature-list">
                  <view v-for="feature in item.features" :key="feature" class="feature">
                    <text class="feature-dot">◆</text>
                    <KeywordText class="feature-text" :text="feature" />
                  </view>
                </view>
              </view>

              <view v-if="draft.classCode === item.code" class="class-ribbon">已选择</view>
            </view>
          </view>
        </view>

        <!-- 子职步骤：卡片式，必须配图 -->
        <view v-else-if="isSubclassStep" class="panel">
          <view class="panel-title">
            <KeywordText :text="currentStep?.title || '选择子职'" />
          </view>
          <view v-if="currentStep?.description" class="muted step-hint">
            <KeywordText :text="currentStep.description" />
          </view>

          <view v-if="subclassesLoading" class="muted">正在加载子职…</view>
          <view v-else-if="subclassesError" class="load-error" @tap="loadSubclasses">
            <text class="muted">子职加载失败，点这里重试</text>
          </view>
          <view v-else-if="!draft.classCode" class="muted">先回上一步选好职业。</view>
          <view v-else-if="!subclasses.length" class="muted subclass-empty">
            「{{ draft.className }}」的子职还在整理中，这一步先跳过，直接点「下一步」。
          </view>

          <view v-else class="subclass-list">
            <view
              v-for="item in subclasses"
              :key="item.code"
              class="subclass-card"
              :class="{ 'subclass-selected': draft.subclassCode === item.code }"
              @tap="pickSubclass(item)"
            >
              <view class="subclass-cover-wrap">
                <image
                  v-if="subclassCover(item) && !failedSubclassCovers[item.code]"
                  class="subclass-cover"
                  :src="subclassCover(item)"
                  mode="aspectFill"
                  @error="failedSubclassCovers[item.code] = true"
                />
                <view
                  v-else
                  class="subclass-cover subclass-cover-fallback"
                  :style="{ backgroundColor: item.accent || DEFAULT_ACCENT }"
                >
                  <text>{{ item.name.slice(0, 1) }}</text>
                </view>
              </view>

              <view class="subclass-body">
                <view class="subclass-name">
                  <KeywordText :text="item.name" />
                </view>
                <view class="subclass-desc">
                  <KeywordText :text="item.description" />
                </view>
                <view class="feature-list subclass-features">
                  <view v-for="feature in item.features" :key="feature" class="feature">
                    <text class="feature-dot">◆</text>
                    <KeywordText class="feature-text" :text="feature" />
                  </view>
                </view>
              </view>

              <view v-if="draft.subclassCode === item.code" class="class-ribbon">已选择</view>
            </view>
          </view>
        </view>

        <!-- 种族步骤：核心种族卡片 -->
        <view v-else-if="isRaceStep" class="panel">
          <view class="panel-title">
            <KeywordText :text="currentStep?.title || '选择种族'" />
          </view>
          <view v-if="currentStep?.description" class="muted step-hint">
            <KeywordText :text="currentStep.description" />
          </view>

          <view
            class="race-house-rule"
            :class="{ 'race-house-rule-on': draft.houseRuleAbilityScores }"
            @tap="toggleHouseRule"
          >
            <view class="house-rule-body">
              <view class="house-rule-title">种族加值：任意两项 +2 / +1</view>
              <view class="house-rule-hint muted">
                开启后不采用各族的固定属性加值，所有种族都改为自选两项属性分别 +2 / +1。
              </view>
            </view>
            <view class="house-rule-switch" :class="{ on: draft.houseRuleAbilityScores }">
              <view class="house-rule-knob" />
            </view>
          </view>

          <view v-if="racesLoading" class="muted">正在加载种族…</view>
          <view v-else-if="racesError" class="load-error" @tap="loadRaces">
            <text class="muted">种族加载失败，点这里重试</text>
          </view>
          <view v-else-if="!races.length" class="muted race-empty">
            这个规则书还没配置种族，这一步可以先跳过，直接点「下一步」。
          </view>

          <view v-else class="race-list">
            <view
              v-for="item in races"
              :key="item.code"
              class="race-card"
              :class="{ 'race-selected': draft.raceCode === item.code }"
              @tap="pickRace(item)"
            >
              <view class="race-head">
                <view class="race-name">
                  <KeywordText :text="item.name" />
                </view>
                <view v-if="draft.raceCode === item.code" class="race-check">✓</view>
              </view>
              <view class="race-meta">
                <text v-if="abilityText(item)" class="race-badge">
                  <KeywordText :text="abilityText(item)" />
                </text>
                <KeywordText class="race-speed" :text="`[速度] ${item.speed} 尺`" />
              </view>
              <view class="race-desc">
                <KeywordText :text="item.description" />
              </view>
              <view v-if="visibleTraits(item).length" class="feature-list race-features">
                <view v-for="trait in visibleTraits(item)" :key="trait" class="feature">
                  <text class="feature-dot">◆</text>
                  <KeywordText class="feature-text" :text="trait" />
                </view>
              </view>
            </view>
          </view>
        </view>

        <!-- 亚种步骤：按种族过滤，没有亚种的种族可跳过 -->
        <view v-else-if="isSubraceStep" class="panel">
          <view class="panel-title">
            <KeywordText :text="currentStep?.title || '选择亚种'" />
          </view>
          <view v-if="currentStep?.description" class="muted step-hint">
            <KeywordText :text="currentStep.description" />
          </view>

          <view v-if="subracesLoading" class="muted">正在加载亚种…</view>
          <view v-else-if="subracesError" class="load-error" @tap="loadSubraces">
            <text class="muted">亚种加载失败，点这里重试</text>
          </view>
          <view v-else-if="!draft.raceCode" class="muted">先回上一步选好种族。</view>
          <view v-else-if="!subraces.length" class="muted subrace-empty">
            「{{ draft.raceName }}」没有可选的亚种，这一步可以直接点「下一步」。
          </view>

          <view v-else class="subrace-list">
            <view
              v-for="item in subraces"
              :key="item.code"
              class="subrace-card"
              :class="{ 'subrace-selected': draft.subraceCode === item.code }"
              @tap="pickSubrace(item)"
            >
              <view class="race-head">
                <view class="race-name">
                  <KeywordText :text="item.name" />
                </view>
                <view v-if="draft.subraceCode === item.code" class="race-check">✓</view>
              </view>
              <view class="race-desc">
                <KeywordText :text="item.description" />
              </view>
              <view v-if="item.traits.length" class="feature-list race-features">
                <view v-for="trait in item.traits" :key="trait" class="feature">
                  <text class="feature-dot">◆</text>
                  <KeywordText class="feature-text" :text="trait" />
                </view>
              </view>
            </view>
          </view>
        </view>

        <!-- 决定背景：第一个是阵营（九宫格），后面再加背景本身 -->
        <view v-else-if="isBackgroundStep" class="panel">
          <view class="panel-title">
            <KeywordText :text="currentStep?.title || '决定背景'" />
          </view>
          <view v-if="currentStep?.description" class="muted step-hint">
            <KeywordText :text="currentStep.description" />
          </view>

          <!-- 阵营 -->
          <view class="bg-block">
            <view class="bg-block-title">阵营</view>
            <view v-if="alignmentsLoading" class="muted">正在加载阵营…</view>
            <view v-else-if="alignmentsError" class="load-error" @tap="loadAlignments">
              <text class="muted">阵营加载失败，点这里重试</text>
            </view>
            <view v-else-if="!alignments.length" class="muted background-empty">
              这个规则书还没配置阵营，这一段可以先跳过。
            </view>
            <view v-else class="background-fields">
              <view
                class="bg-field"
                :class="{ 'bg-field-filled': Boolean(pickedAlignment) }"
                @tap="openAlignmentSheet"
              >
                <view class="bg-field-body">
                  <view class="bg-field-label">阵营</view>
                  <view v-if="pickedAlignment" class="bg-field-value">
                    <image
                      v-if="pickedAlignment.iconUrl && !failedAlignmentIcons[pickedAlignment.code]"
                      class="bg-field-icon"
                      :src="pickedAlignment.iconUrl"
                      mode="aspectFit"
                      @error="markAlignmentIconFailed(pickedAlignment.code)"
                    />
                    <view
                      v-else
                      class="bg-field-icon bg-field-icon-fallback"
                      :style="{ backgroundColor: pickedAlignment.accent || DEFAULT_ACCENT }"
                    >
                      <text>{{ pickedAlignment.name.slice(0, 1) }}</text>
                    </view>
                    <view class="bg-field-name" @tap.stop>
                      <KeywordText :text="'[' + pickedAlignment.name + ']'" />
                    </view>
                  </view>
                  <view v-else class="bg-field-value muted">点这里，从九宫格里挑一个</view>
                </view>
                <text class="bg-field-arrow">›</text>
              </view>
              <view class="muted bg-field-tip">
                点阵营名可以看词条解释；守序 ↔ 混乱是做事的方式，善良 ↔ 邪恶是动机。
              </view>
            </view>
          </view>

          <!-- 背景 -->
          <view class="bg-block">
            <view class="bg-block-title">背景</view>
            <view v-if="!backgroundCatalog.length" class="muted">正在加载背景…</view>
            <view v-else class="bg-cards">
              <view
                v-for="item in backgroundCatalog"
                :key="item.id"
                class="bg-card"
                :class="{ 'bg-card-selected': draft.backgroundCode === item.id }"
                @tap="pickBackground(item)"
              >
                <view class="bg-card-head">
                  <view class="bg-card-name">
                    <KeywordText :text="'[' + item.name + ']'" />
                  </view>
                  <view class="bg-card-actions">
                    <text class="bg-card-detail" @tap.stop="openBackgroundDetail(item)">详情 ›</text>
                    <view v-if="draft.backgroundCode === item.id" class="race-check">✓</view>
                  </view>
                </view>
                <view class="bg-card-desc muted">{{ item.description }}</view>
              </view>

              <view
                class="bg-card bg-card-custom"
                :class="{ 'bg-card-selected': draft.backgroundCode === CUSTOM_BACKGROUND_CODE }"
                @tap="pickBackground({ id: CUSTOM_BACKGROUND_CODE, name: '自定义背景', description: '自己定技能、工具、语言、装备和特性；填多少都按 DM 同意处理。', detail: '', skills: [], tools: '', languageCount: 0, equipment: '', featureName: '', featureText: '' })"
              >
                <view class="bg-card-head">
                  <view class="bg-card-name">自定义背景</view>
                  <view v-if="draft.backgroundCode === CUSTOM_BACKGROUND_CODE" class="race-check">✓</view>
                </view>
                <view class="bg-card-desc muted">自己定技能、工具、语言、装备和特性；填多少都按 DM 同意处理。</view>
              </view>
            </view>

            <view v-if="draft.backgroundCode === CUSTOM_BACKGROUND_CODE" class="custom-bg">
              <view class="desc-field">
                <text class="desc-label muted">名称</text>
                <input
                  class="desc-input"
                  :value="draft.customBackground.name"
                  placeholder="例如：走私船船长"
                  @input="setCustomBackgroundText('name', $event.detail.value)"
                />
              </view>
              <view class="desc-field">
                <text class="desc-label muted">描述</text>
                <input
                  class="desc-input"
                  :value="draft.customBackground.description"
                  placeholder="一句话说明"
                  @input="setCustomBackgroundText('description', $event.detail.value)"
                />
              </view>
              <view class="desc-field custom-skills">
                <KeywordText class="desc-label muted" :text="'[技能熟练]'" />
                <view class="skill-grid">
                  <view
                    v-for="skill in skillRows"
                    :key="skill.id"
                    class="skill-chip"
                    :class="{ 'skill-chosen': draft.customBackground.skills.includes(skill.name) }"
                    @tap="toggleCustomSkill(skill.name)"
                  >
                    <KeywordText :text="'[' + skill.name + ']'" />
                  </view>
                </view>
              </view>
              <view class="desc-field">
                <text class="desc-label muted">工具</text>
                <input
                  class="desc-input"
                  :value="draft.customBackground.tools"
                  placeholder="例如：盗贼工具、航海工具"
                  @input="setCustomBackgroundText('tools', $event.detail.value)"
                />
              </view>
              <view class="desc-field">
                <text class="desc-label muted">额外语言</text>
                <input
                  class="desc-input"
                  type="number"
                  :value="String(draft.customBackground.languageCount)"
                  placeholder="0"
                  @input="setCustomLanguageCount($event.detail.value)"
                />
              </view>
              <view class="desc-field">
                <text class="desc-label muted">装备</text>
                <input
                  class="desc-input"
                  :value="draft.customBackground.equipment"
                  placeholder="例如：匕首、绳索、15 gp"
                  @input="setCustomBackgroundText('equipment', $event.detail.value)"
                />
              </view>
              <view class="desc-field">
                <text class="desc-label muted">特性名</text>
                <input
                  class="desc-input"
                  :value="draft.customBackground.featureName"
                  placeholder="例如：船运人脉"
                  @input="setCustomBackgroundText('featureName', $event.detail.value)"
                />
              </view>
              <view class="desc-field">
                <text class="desc-label muted">特性说明</text>
                <input
                  class="desc-input"
                  :value="draft.customBackground.featureText"
                  placeholder="这个特性做什么"
                  @input="setCustomBackgroundText('featureText', $event.detail.value)"
                />
              </view>
            </view>
          </view>

          <!-- 语言 -->
          <view class="bg-block">
            <view class="bg-block-title">语言</view>
            <view class="muted option-hint">
              种族母语：{{ grantedLanguages.join('、') || '无' }}；背景额外 {{ backgroundOption?.languageCount ?? 0 }} 门，种族额外 {{ raceOption?.languageChoiceCount ?? 0 }} 门。可以留空。
            </view>
            <view v-if="!languageCatalog.length" class="muted">正在加载语言…</view>
            <view v-else class="skill-grid">
              <view
                v-for="row in languageRows"
                :key="row.id"
                class="skill-chip"
                :class="{ 'skill-granted': row.granted, 'skill-chosen': row.chosen }"
                @tap="pickLanguage(row.name)"
              >
                <text>{{ row.name }}</text>
                <text class="skill-ability muted">{{ row.kind }}</text>
              </view>
            </view>
            <view class="muted option-count">
              已选 {{ draft.selectedLanguages.length }} / 最多 {{ languagePickCount }} 门（可以不选）
            </view>
          </view>

          <!-- 描述角色 -->
          <view class="bg-block">
            <view class="bg-block-title">描述角色</view>
            <view class="desc-field">
              <text class="desc-label muted">名字</text>
              <view class="desc-name-row">
                <input
                  class="desc-input"
                  :value="draft.characterName"
                  placeholder="留空则提交时随机"
                  @input="setDraftText('characterName', $event.detail.value)"
                />
                <view class="desc-random" @tap="randomCharacterName">随机</view>
              </view>
            </view>
            <view class="desc-field">
              <text class="desc-label muted">性别</text>
              <input
                class="desc-input"
                :value="draft.gender"
                placeholder="男 / 女 / 其他"
                @input="setDraftText('gender', $event.detail.value)"
              />
            </view>
            <view class="desc-field">
              <text class="desc-label muted">年龄</text>
              <input
                class="desc-input"
                type="number"
                :value="draft.age ? String(draft.age) : ''"
                placeholder="例如 25"
                @input="setDraftAge($event.detail.value)"
              />
            </view>
            <view class="desc-field">
              <text class="desc-label muted">身高</text>
              <input
                class="desc-input"
                :value="draft.height"
                placeholder="例如 5 尺 8 寸"
                @input="setDraftText('height', $event.detail.value)"
              />
            </view>
            <view class="desc-field">
              <text class="desc-label muted">体重</text>
              <input
                class="desc-input"
                :value="draft.weight"
                placeholder="例如 150 磅"
                @input="setDraftText('weight', $event.detail.value)"
              />
            </view>
            <view class="desc-field">
              <text class="desc-label muted">外貌</text>
              <textarea
                class="desc-textarea"
                :value="draft.appearance"
                placeholder="发色、眼睛、衣着、习惯动作…（可以留空）"
                @input="setDraftText('appearance', $event.detail.value)"
              />
            </view>
            <view class="desc-field">
              <text class="desc-label muted">性格特质</text>
              <input
                class="desc-input"
                :value="draft.trait"
                placeholder="一句话性格（可以留空）"
                @input="setDraftText('trait', $event.detail.value)"
              />
            </view>
            <view class="desc-field">
              <text class="desc-label muted">理想</text>
              <input
                class="desc-input"
                :value="draft.ideal"
                placeholder="角色相信什么（可以留空）"
                @input="setDraftText('ideal', $event.detail.value)"
              />
            </view>
            <view class="desc-field">
              <text class="desc-label muted">牵绊</text>
              <input
                class="desc-input"
                :value="draft.bond"
                placeholder="放不下的人或事（可以留空）"
                @input="setDraftText('bond', $event.detail.value)"
              />
            </view>
            <view class="desc-field">
              <text class="desc-label muted">缺点</text>
              <input
                class="desc-input"
                :value="draft.flaw"
                placeholder="弱点或秘密（可以留空）"
                @input="setDraftText('flaw', $event.detail.value)"
              />
            </view>
          </view>
        </view>

        <!-- 决定属性：六项数值，点每张卡看「来源」 -->
        <view v-else-if="isAttributesStep" class="panel">
          <view class="attr-head">
            <view class="panel-title attr-head-title">
              <KeywordText :text="currentStep?.title || '决定属性'" />
            </view>
            <button
              class="btn attr-roll"
              hover-class="btn-pressed"
              hover-stay-time="80"
              :disabled="attributeRolling"
              @tap="rollAttributeValues"
            >
              {{ attributeRolling ? '投掷中…' : '投掷' }}
            </button>
          </view>
          <view v-if="currentStep?.description" class="muted step-hint">
            <KeywordText :text="currentStep.description" />
          </view>

          <view v-if="!attributeRows.length" class="muted">这个规则书还没配置属性模板。</view>

          <template v-else>
            <view v-if="attributeRolling" class="muted attr-rolling">正在投掷…</view>
            <view v-else-if="attributeError" class="load-error" @tap="rollAttributeValues">
              <text class="muted">投掷失败，点这里重试</text>
            </view>

            <view class="attr-grid">
              <view
                v-for="row in attributeRows"
                :key="row.key"
                class="attr-card"
                :class="{ 'attr-card-ready': row.base > 0 }"
              >
                <view class="attr-card-head">
                  <KeywordText class="attr-name" :text="'[' + row.label + ']'" />
                </view>
                <view class="attr-input-wrap">
                  <input
                    class="attr-input"
                    type="number"
                    :value="row.base > 0 ? String(row.base) : ''"
                    placeholder="—"
                    placeholder-style="color:#c9bca8"
                    @input="setAttributeValue(row.key, $event.detail.value)"
                  />
                </view>
              </view>
            </view>

            <!-- 半精灵这类「任选两项 +1」 -->
            <view v-if="raceChoice" class="attr-choice">
              <view class="attr-choice-title">
                <KeywordText :text="raceOption?.name" />：{{ raceChoiceLabel }}
              </view>
              <view class="attr-choice-hint muted">
                已选 {{ chosenRaceChoices.length }} / {{ raceChoice.count }} 项
              </view>
              <view class="attr-chips">
                <view
                  v-for="row in attributeRows"
                  :key="row.key"
                  class="attr-chip"
                  :class="{ 'attr-chip-on': chosenRaceChoices.includes(row.key) }"
                  @tap="toggleRaceChoice(row.key)"
                >
                  {{ row.label }}
                </view>
              </view>
            </view>

            <!-- 种族加值改为「任意两项 +2 / +1」时，直接在这里选 -->
            <view v-if="draft.houseRuleAbilityScores" class="attr-choice">
              <view class="attr-choice-title">
                <KeywordText :text="raceOption?.name" />：任意两项 +2 / +1
              </view>
              <view class="attr-choice-hint muted">先挑 +2 的那项，再挑 +1 的那项。</view>
              <view class="attr-choice-row">
                <text class="attr-chip-label">+2</text>
                <view class="attr-chips">
                  <view
                    v-for="row in attributeRows"
                    :key="row.key"
                    class="attr-chip"
                    :class="{ 'attr-chip-on': housePlus2 === row.key }"
                    @tap="pickHouseBonus('housePlus2', row.key)"
                  >
                    {{ row.label }}
                  </view>
                </view>
              </view>
              <view class="attr-choice-row">
                <text class="attr-chip-label">+1</text>
                <view class="attr-chips">
                  <view
                    v-for="row in attributeRows"
                    :key="row.key"
                    class="attr-chip"
                    :class="{ 'attr-chip-on': housePlus1 === row.key }"
                    @tap="pickHouseBonus('housePlus1', row.key)"
                  >
                    {{ row.label }}
                  </view>
                </view>
              </view>
            </view>

            <view v-if="hasFilledAttributes" class="attr-final">
              <view class="attr-final-head">
                <view class="attr-final-title">最终属性</view>
                <view class="attr-final-hint muted">初始值 + 种族 / 亚种加值，点任意一项看来源</view>
              </view>
              <view class="attr-final-grid">
                <view
                  v-for="row in attributeRows"
                  :key="row.key"
                  class="attr-final-cell"
                  :class="{ 'attr-final-empty': !row.base }"
                  @tap="openAttributeSheet(row.key)"
                >
                  <view class="attr-final-value">{{ row.base > 0 ? row.final : '—' }}</view>
                  <view class="attr-final-label muted">{{ row.label }}</view>
                </view>
              </view>
            </view>

            <view v-if="attributeSnapshot.length" class="attr-snapshot">
              <view
                v-for="item in attributeSnapshot"
                :key="item.key"
                class="attr-snap"
                @tap="openStatSheet(item.key)"
              >
                <view class="attr-snap-value">{{ item.value }}</view>
                <view class="attr-snap-label muted">{{ item.label }}</view>
                <view class="attr-snap-link">来源 ›</view>
              </view>
            </view>
          </template>
        </view>

        <!-- 技能与特性 -->
        <view v-else-if="isSkillsStep" class="panel">
          <view class="panel-title"><KeywordText :text="currentStep?.title || '技能与特性'" /></view>
          <view v-if="currentStep?.description" class="muted step-hint">
            <KeywordText :text="currentStep.description" />
          </view>
          <view class="muted option-hint">
            <KeywordText :text="skillHintText" />
          </view>
          <view v-if="!skillCatalog.length" class="muted">正在加载技能…</view>
          <view v-else class="skill-grid">
            <view
              v-for="row in skillRows"
              :key="row.id"
              class="skill-chip"
              :class="{
                'skill-granted': row.granted,
                'skill-chosen': row.chosen,
                'skill-locked': !row.granted && !row.selectable,
              }"
              @tap="pickSkill(row.name)"
            >
              <KeywordText :text="'[' + row.name + ']'" />
              <KeywordText class="skill-ability muted" :text="'[' + row.ability + ']'" />
            </view>
          </view>
          <view class="muted option-count">已选 {{ draft.selectedSkills.length }} / {{ skillPickCount }}</view>

          <view v-if="featureGroups.length" class="feature-block">
            <view class="bg-block-title">
              <KeywordText :text="'[职业特性]'" />
            </view>
            <view v-for="group in featureGroups" :key="group.type" class="feature-group">
              <view class="bg-section-title">
                <KeywordText :text="'[' + group.label + ']'" />
              </view>
              <view class="skill-grid">
                <view
                  v-for="option in group.options"
                  :key="option.name"
                  class="skill-chip"
                  :class="{ 'skill-chosen': draft.featureChoices[group.type] === option.name }"
                  @tap="pickFeature(group.type, option.name)"
                >
                  <KeywordText :text="'[' + option.name + ']'" />
                </view>
              </view>
            </view>
            <view class="muted option-count">每个类别选 1 项</view>
          </view>
        </view>

        <!-- 起始装备 -->
        <view v-else-if="isEquipmentStep" class="panel">
          <view class="panel-title"><KeywordText :text="currentStep?.title || '起始装备'" /></view>
          <view v-if="currentStep?.description" class="muted step-hint">
            <KeywordText :text="currentStep.description" />
          </view>
          <view v-if="!equipmentCatalog" class="muted">正在加载装备…</view>
          <view v-else-if="!equipmentKits.length" class="muted">这个职业还没配置起始装备，直接下一步。</view>
          <template v-else>
            <view class="bg-section-title">起始套装</view>
            <view class="equip-kits">
              <view
                v-for="kit in equipmentKits"
                :key="kit.kitId"
                class="equip-kit-card"
                :class="{ 'equip-kit-on': selectedKit?.kitId === kit.kitId }"
                @tap="pickKit(kit.kitId)"
              >
                <view class="equip-kit-head">
                  <text class="equip-kit-name">{{ kit.name }}</text>
                  <view v-if="selectedKit?.kitId === kit.kitId" class="race-check">✓</view>
                </view>
                <view class="equip-kit-line muted">{{ kitSummary(kit) }}</view>
              </view>
            </view>

            <view class="equip-kit">
              <view class="equip-row"><text class="muted">护甲</text><text>{{ armorOption?.name || '无甲' }}</text></view>
              <view class="equip-row"><text class="muted">盾</text><text>{{ selectedKit?.shield ? '有（AC +2）' : '无' }}</text></view>
              <view class="equip-row"><text class="muted">装备包</text><text>{{ selectedKit?.pack || '无' }}</text></view>
            </view>
            <view class="bg-section-title">武器（选一把当主手，其余随身携带）</view>
            <view class="skill-grid">
              <view
                v-for="weapon in weaponOptions"
                :key="weapon.id"
                class="skill-chip"
                :class="{ 'skill-chosen': selectedWeapon?.id === weapon.id }"
                @tap="pickWeapon(weapon.id)"
              >
                <text>{{ weapon.name }}</text>
                <text class="skill-ability muted">{{ weapon.damage }}</text>
              </view>
            </view>
            <view class="equip-stats">
              <view class="equip-stat">
                <text class="equip-stat-value">{{ combatSummary.hitPoints }}</text>
                <text class="muted">生命上限</text>
              </view>
              <view class="equip-stat">
                <text class="equip-stat-value">{{ combatSummary.armorClass }}</text>
                <text class="muted">AC</text>
              </view>
              <view class="equip-stat">
                <text class="equip-stat-value">{{ signed(combatSummary.initiative) }}</text>
                <text class="muted">先攻</text>
              </view>
              <view class="equip-stat">
                <text class="equip-stat-value">{{ signed(combatSummary.attackBonus) }}</text>
                <text class="muted">{{ combatSummary.weaponName }}攻击</text>
              </view>
            </view>
          </template>
        </view>

        <!-- 其它类型的步骤：面板还没做，先按配置占位 -->
        <view v-else class="panel placeholder-panel">
          <view class="panel-title">
            <KeywordText :text="currentStep?.title" />
          </view>
          <KeywordText class="muted" :text="currentStep?.description || '这一步的面板还在开发中，先跟着流程往下走。'" />
          <text class="placeholder-kind muted">步骤类型：{{ currentStep?.kind }}</text>
        </view>
      </template>
    </template>

    <!-- 右侧索引：滑动时展开，停下后缩成一条细线，不挡着读卡片 -->
    <view
      v-if="railOptions.length > 1"
      class="class-rail"
      :class="{ 'rail-idle': !railOpen }"
      @tap="wakeRail"
    >
      <view
        v-for="(item, index) in railOptions"
        :key="item.code"
        class="rail-item"
        :class="{ active: activeRailIndex === index }"
        @tap="scrollToRail(index)"
      >
        <KeywordText class="rail-name" :text="item.name" />
        <text v-if="railSelectedCode === item.code" class="rail-check">✓</text>
      </view>
    </view>

    <!-- 流程操作固定在右下角：种族 / 亚种这类长列表不用滚到底，每一步都有 -->
    <view v-if="flowActive" class="wizard-dock">
      <button
        class="btn btn-plain"
        hover-class="btn-pressed"
        hover-stay-time="80"
        :disabled="finishing"
        @tap="prev"
      >
        上一步
      </button>
      <button
        class="btn wizard-dock-next"
        hover-class="btn-pressed"
        hover-stay-time="80"
        :disabled="!canNext || finishing"
        @tap="next"
      >
        {{ nextLabel }}
      </button>
    </view>

    <!-- 阵营九宫格：点阵营那一栏弹出来选，选完自动收起 -->
    <view v-if="alignmentSheetVisible" class="align-mask" @tap="closeAlignmentSheet">
      <view class="align-sheet" @tap.stop>
        <view class="align-sheet-head">
          <view class="align-sheet-title">阵营</view>
          <text class="align-sheet-close" @tap="closeAlignmentSheet">×</text>
        </view>
        <view class="muted align-sheet-hint">
          守序 ↔ 混乱是做事的方式，善良 ↔ 邪恶是动机；点右上角的「i」看每格的解释。
        </view>

        <view class="align-grid">
          <view
            v-for="item in alignments"
            :key="item.code"
            class="align-cell"
            :class="{ 'align-cell-selected': draft.alignmentCode === item.code }"
            :style="{ borderTopColor: item.accent || DEFAULT_ACCENT }"
            @tap="pickAlignment(item)"
          >
            <image
              v-if="item.iconUrl && !failedAlignmentIcons[item.code]"
              class="align-icon"
              :src="item.iconUrl"
              mode="aspectFit"
              @error="markAlignmentIconFailed(item.code)"
            />
            <view
              v-else
              class="align-icon align-icon-fallback"
              :style="{ backgroundColor: item.accent || DEFAULT_ACCENT }"
            >
              <text>{{ item.name.slice(0, 1) }}</text>
            </view>
            <view class="align-name">{{ item.name }}</view>
            <view
              v-if="hasAlignmentKeyword(item.name)"
              class="align-info"
              @tap.stop="openAlignmentKeyword(item)"
            >i</view>
            <view v-if="draft.alignmentCode === item.code" class="align-check">✓</view>
          </view>
        </view>
      </view>
    </view>

    <!-- 背景详情：点「详情」看技能 / 工具 / 语言 / 装备 / 特性 -->
    <view v-if="backgroundDetailVisible && backgroundDetailItem" class="attr-mask" @tap="closeBackgroundDetail">
      <view class="attr-sheet" @tap.stop>
        <view class="attr-sheet-head">
          <view class="attr-sheet-title">
            <KeywordText :text="'[' + backgroundDetailItem.name + ']'" />
          </view>
          <text class="attr-sheet-close" @tap="closeBackgroundDetail">×</text>
        </view>
        <view v-if="backgroundDetailItem.description" class="bg-detail-desc muted">
          {{ backgroundDetailItem.description }}
        </view>
        <view v-if="backgroundDetailItem.detail" class="bg-detail-long">
          <view class="bg-detail-long-title">详细介绍</view>
          <view class="bg-detail-long-text">{{ backgroundDetailItem.detail }}</view>
        </view>
        <view class="bg-grant">
          <view class="bg-grant-row">
            <KeywordText class="muted" :text="'[技能熟练]'" />
            <text>{{ backgroundDetailItem.skills.join('、') || '无' }}</text>
          </view>
          <view class="bg-grant-row">
            <text class="muted">工具</text>
            <text>{{ backgroundDetailItem.tools || '无' }}</text>
          </view>
          <view class="bg-grant-row">
            <text class="muted">额外语言</text>
            <text>{{ backgroundDetailItem.languageCount }} 门</text>
          </view>
          <view class="bg-grant-row">
            <text class="muted">装备</text>
            <text>{{ backgroundDetailItem.equipment || '无' }}</text>
          </view>
          <view class="bg-grant-row">
            <text class="muted">背景特性</text>
            <text>
              {{ backgroundDetailItem.featureName
                ? `${backgroundDetailItem.featureName}：${backgroundDetailItem.featureText}`
                : '无' }}
            </text>
          </view>
        </view>
        <button class="btn attr-sheet-done" hover-class="btn-pressed" hover-stay-time="80" @tap="closeBackgroundDetail">
          完成
        </button>
      </view>
    </view>

    <!-- 属性来源：点属性的卡弹出来，一项项说清楚这个数怎么来的 -->
    <view v-if="attributeSheetVisible && attributeSheetRow" class="attr-mask" @tap="closeAttributeSheet">
      <view class="attr-sheet" @tap.stop>
        <view class="attr-sheet-head">
          <view class="attr-sheet-title">
            <KeywordText :text="'[' + attributeSheetRow.label + ']'" />
          </view>
          <text class="attr-sheet-close" @tap="closeAttributeSheet">×</text>
        </view>

        <view class="attr-sheet-total">
          <text class="attr-sheet-number">{{ attributeSheetRow.base > 0 ? attributeSheetRow.final : '—' }}</text>
          <text class="muted attr-sheet-mod">
            调整值 {{ signed(attributeSheetRow.modifier) }}
          </text>
        </view>

        <view class="attr-sources">
          <view v-for="(source, index) in attributeSheetRow.sources" :key="index" class="attr-source">
            <view class="attr-source-main">
              <text class="attr-source-label">{{ source.label }}</text>
              <text class="attr-source-value" :class="{ 'attr-source-plus': source.label !== '初始' }">
                {{ source.label === '初始' ? source.value : signed(source.value) }}
              </text>
            </view>
            <view class="attr-source-note muted">{{ source.note }}</view>
          </view>
          <view v-if="!attributeSheetRow.sources.length" class="muted">还没有数值。</view>
        </view>

        <button class="btn attr-sheet-done" hover-class="btn-pressed" hover-stay-time="80" @tap="closeAttributeSheet">
          完成
        </button>
      </view>
    </view>

    <!-- 衍生数值来源：生命值 / AC / 先攻 / 被动察觉，点一下看怎么算出来的 -->
    <view v-if="statSheetVisible && statSheetRow" class="attr-mask" @tap="closeStatSheet">
      <view class="attr-sheet" @tap.stop>
        <view class="attr-sheet-head">
          <view class="attr-sheet-title">{{ statSheetRow.label }}</view>
          <text class="attr-sheet-close" @tap="closeStatSheet">×</text>
        </view>

        <view class="attr-sheet-total">
          <text class="attr-sheet-number">{{ statSheetRow.value }}</text>
        </view>

        <view class="attr-sources">
          <view v-for="(source, index) in statSheetRow.sources" :key="index" class="attr-source">
            <view class="attr-source-main">
              <text class="attr-source-label">{{ source.label }}</text>
              <text class="attr-source-value" :class="{ 'attr-source-plus': source.plus }">
                {{ source.plus ? signed(source.value) : source.value }}
              </text>
            </view>
            <view class="attr-source-note muted">{{ source.note }}</view>
          </view>
        </view>

        <button class="btn attr-sheet-done" hover-class="btn-pressed" hover-stay-time="80" @tap="closeStatSheet">
          完成
        </button>
      </view>
    </view>

    <KeywordModal />

    <ClassDetailModal
      :visible="detailVisible"
      :option="detailClass"
      :system-code="draft.systemCode"
      @close="detailVisible = false"
    />
  </view>
</template>

<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { onHide, onLoad, onPageScroll, onUnload } from '@dcloudio/uni-app'
import { request } from '../../api/http'
import type {
  AbilityScoresResponse,
  AlignmentOption,
  BackgroundDetail,
  ClassOption,
  CreationFlow,
  CreationStep,
  EquipmentKit,
  EquipmentResponse,
  FeatureOption,
  GameSystem,
  LanguageOption,
  RaceOption,
  SkillOption,
  SubclassOption,
  SubraceOption,
  WeaponOption,
} from '../../api/types'
import { clearDraft, emptyDraft, loadDraft, saveDraft, type CreateDraft } from '../../stores/createDraft'
import { ensureKeywords, findKeyword, openKeyword } from '../../stores/keywords'
import AppHeader from '../../components/AppHeader/AppHeader.vue'
import KeywordText from '../../components/KeywordText/KeywordText.vue'
import KeywordModal from '../../components/KeywordModal/KeywordModal.vue'
import ClassDetailModal from '../../components/ClassDetailModal/ClassDetailModal.vue'

/** 内容里没配主色时的兜底（纯展示默认值，不是内容） */
const DEFAULT_ACCENT = '#5b4636'

/** 规则系统没配属性模板时的兜底（DND 5E 六维） */
const DEFAULT_ATTRIBUTE_TEMPLATES = [
  { key: 'STR', label: '力量', formula: '4d6kh3' },
  { key: 'DEX', label: '敏捷', formula: '4d6kh3' },
  { key: 'CON', label: '体质', formula: '4d6kh3' },
  { key: 'INT', label: '智力', formula: '4d6kh3' },
  { key: 'WIS', label: '感知', formula: '4d6kh3' },
  { key: 'CHA', label: '魅力', formula: '4d6kh3' },
]

interface AttributeSource {
  label: string
  note: string
  value: number
  short?: string
}

interface AttributeRow {
  key: string
  label: string
  base: number
  final: number
  modifier: number
  formula: string
  sources: AttributeSource[]
}

/** 衍生数值的来源：基础值不带符号，加值显示 +N */
interface StatSource {
  label: string
  note: string
  value: number
  plus: boolean
}

/** 面板底部的衍生数值：值 + 怎么算出来的 */
interface StatRow {
  key: string
  label: string
  value: string
  sources: StatSource[]
}

const draft = ref<CreateDraft>(emptyDraft())
const systems = ref<GameSystem[]>([])
const steps = ref<CreationStep[]>([])
const classes = ref<ClassOption[]>([])
const subclasses = ref<SubclassOption[]>([])
const races = ref<RaceOption[]>([])
const subraces = ref<SubraceOption[]>([])
/** 阵营（九宫格）：不跟随职业 / 种族，单独一份 */
const alignments = ref<AlignmentOption[]>([])
const alignmentsLoaded = ref(false)
const alignmentSheetVisible = ref(false)
/** 决定属性：生成中标记、属性来源弹窗 */
const attributeRolling = ref(false)
const attributeError = ref(false)
const attributeSheetVisible = ref(false)
const attributeSheetKey = ref<string | null>(null)
/** 衍生数值（生命值 / AC / 先攻 / 被动察觉）的来源弹窗 */
const statSheetVisible = ref(false)
const statSheetKey = ref<string | null>(null)
/** 子职 / 亚种列表是按哪个职业 / 种族加载的，用来判断「没有内容」是不是真的 */
const subclassCatalogCode = ref<string | null>(null)
const subraceCatalogCode = ref<string | null>(null)
const flowLoading = ref(false)
const flowError = ref(false)
const classesLoading = ref(false)
const classesError = ref(false)
const subclassesLoading = ref(false)
const subclassesError = ref(false)
const racesLoading = ref(false)
const racesError = ref(false)
const subracesLoading = ref(false)
const subracesError = ref(false)
const alignmentsLoading = ref(false)
const alignmentsError = ref(false)
const ready = ref(false)
const started = ref(false)
const finishing = ref(false)
const failedIcons = ref<Record<string, boolean>>({})
const failedCovers = ref<Record<string, boolean>>({})
const failedSubclassCovers = ref<Record<string, boolean>>({})
const failedAlignmentIcons = ref<Record<string, boolean>>({})
const activeRailIndex = ref(0)
const railTops = ref<number[]>([])
const jumping = ref(false)
/** 右侧索引是否展开：滑动时展开，停下滑动后缩成一条细线，不挡着读卡片 */
const railOpen = ref(false)
const RAIL_IDLE_MS = 1000
let railIdleTimer: ReturnType<typeof setTimeout> | null = null
let railWakeMutedUntil = 0
const detailVisible = ref(false)
const detailClass = ref<ClassOption | null>(null)

/** 顶部步骤条：最多露出 3 个，横向拖动查看前后的步骤 */
const stepperWidth = ref(0)
const stepperOffset = ref(0)
const stepperDragging = ref(false)
const stepperStepWidth = computed(() => (stepperWidth.value > 0 ? stepperWidth.value / 3 : 0))
const stepperMaxStart = computed(() => Math.max(0, steps.value.length - 3))
let stepperStartX = 0
let stepperBaseOffset = 0
let stepperMoved = false
let stepperTapLock = false
let stepperMeasureRetries = 0

let jumpTimer: ReturnType<typeof setTimeout> | null = null
let scrollTimer: ReturnType<typeof setTimeout> | null = null

const step = computed(() => draft.value.step)
const currentStep = computed<CreationStep | null>(() => steps.value[draft.value.step - 1] ?? null)
const isClassStep = computed(() => currentStep.value?.kind === 'class')
const isSubclassStep = computed(() => currentStep.value?.kind === 'subclass')
const isRaceStep = computed(() => currentStep.value?.kind === 'race')
const isSubraceStep = computed(() => currentStep.value?.kind === 'subrace')
const isBackgroundStep = computed(() => currentStep.value?.kind === 'background')
const isAttributesStep = computed(() => currentStep.value?.kind === 'attributes')
const attributeTemplates = computed(() => {
  const system = systems.value.find((item) => item.code === draft.value.systemCode)
  return system?.attributes?.length ? system.attributes : DEFAULT_ATTRIBUTE_TEMPLATES
})
const raceOption = computed<RaceOption | null>(
  () => races.value.find((item) => item.code === draft.value.raceCode) ?? null,
)
const subraceOption = computed<SubraceOption | null>(
  () => subraces.value.find((item) => item.code === draft.value.subraceCode) ?? null,
)
/** 半精灵这类「任选两项 +1」；开了可选规则时改用自选 +2/+1 */
const raceChoice = computed(() =>
  draft.value.houseRuleAbilityScores ? null : raceOption.value?.abilityChoices?.[0] ?? null,
)
const chosenRaceChoices = computed(() => draft.value.attributeChoices['race'] ?? [])
/** 「任选两项+1」显示成「任选两项 +1」 */
const raceChoiceLabel = computed(() => (raceChoice.value?.label ?? '').replace(/\+/g, ' + ').trim())
const housePlus2 = computed(() => draft.value.attributeChoices['housePlus2']?.[0] ?? '')
const housePlus1 = computed(() => draft.value.attributeChoices['housePlus1']?.[0] ?? '')
/** 六项属性：初始数值 + 种族 / 亚种 / 自选加值，每一项都带着来源 */
const attributeRows = computed<AttributeRow[]>(() =>
  attributeTemplates.value.map((template) => {
    const base = draft.value.attributeValues[template.key] ?? 0
    const sources: AttributeSource[] = []

    if (base > 0) {
      sources.push({ label: '初始', note: '车卡时填写', value: base })
    }

    if (draft.value.houseRuleAbilityScores) {
      const raceName = raceOption.value?.name ?? '种族'

      if (housePlus2.value === template.key) {
        sources.push({
          label: `种族「${raceName}」`,
          note: '任意两项 +2 / +1',
          value: 2,
          short: '种族 +2',
        })
      }

      if (housePlus1.value === template.key) {
        sources.push({
          label: `种族「${raceName}」`,
          note: '任意两项 +2 / +1',
          value: 1,
          short: '种族 +1',
        })
      }
    } else {
      const race = raceOption.value
      if (race) {
        for (const bonus of race.abilityBonuses ?? []) {
          if (bonus.key === template.key) {
            sources.push({
              label: `种族「${race.name}」`,
              note: `${bonus.label} +${bonus.value}`,
              value: bonus.value,
              short: `${race.name} +${bonus.value}`,
            })
          }
        }

        if (raceChoice.value && chosenRaceChoices.value.includes(template.key)) {
          sources.push({
            label: `种族「${race.name}」`,
            note: raceChoice.value.label,
            value: raceChoice.value.value,
            short: `自选 +${raceChoice.value.value}`,
          })
        }
      }

      const subrace = subraceOption.value
      if (subrace) {
        for (const bonus of subrace.abilityBonuses ?? []) {
          if (bonus.key === template.key) {
            sources.push({
              label: `亚种「${subrace.name}」`,
              note: `${bonus.label} +${bonus.value}`,
              value: bonus.value,
              short: `${subrace.name} +${bonus.value}`,
            })
          }
        }
      }
    }

    const final = sources.reduce((sum, source) => sum + source.value, 0)

    return {
      key: template.key,
      label: template.label,
      base,
      final,
      modifier: Math.floor((final - 10) / 2),
      formula: sources
        .map((source) =>
          source.label === '初始' ? `初始 ${source.value}` : `${source.short ?? source.label} +${source.value}`,
        )
        .join(' + '),
      sources,
    }
  }),
)
/** 有没有填过初始属性：填了才显示「最终属性」栏 */
const hasFilledAttributes = computed(() => attributeRows.value.some((row) => row.base > 0))
/** 面板底部的小结：生命值上限 / 无甲 AC / 先攻 / 被动察觉；每项都能点开看怎么算出来的 */
const attributeSnapshot = computed<StatRow[]>(() => {
  const rows = attributeRows.value
  if (!rows.length || rows.some((row) => !row.base)) {
    return []
  }

  const rowOf = (key: string) => rows.find((row) => row.key === key)
  const dexRow = rowOf('DEX')
  const conRow = rowOf('CON')
  const wisRow = rowOf('WIS')
  const dex = dexRow?.modifier ?? 0
  const con = conRow?.modifier ?? 0
  const wis = wisRow?.modifier ?? 0
  const dexNote = `敏捷 ${dexRow?.final ?? 10} → 调整值 ${signed(dex)}`

  const items: StatRow[] = []

  const className = classOption.value?.name ?? '职业'
  const hitDie = classOption.value?.hitDie ?? ''
  const sides = Number(hitDie.replace(/\D/g, ''))
  if (sides > 0) {
    items.push({
      key: 'hitPoints',
      label: '生命值上限',
      value: String(Math.max(1, sides + con)),
      sources: [
        { label: '职业生命骰', note: `${className} ${hitDie}，1 级直接取满`, value: sides, plus: false },
        { label: '体质调整值', note: `体质 ${conRow?.final ?? 10} → 调整值 ${signed(con)}`, value: con, plus: true },
      ],
    })
  }

  // AC 和装备页一致：职业起始护甲 + 盾；没甲时武僧走无甲防御，其它职业 10 + 敏捷
  const armor = armorOption.value
  const shield = selectedKit.value?.shield ? 2 : 0
  const acSources: StatSource[] = []
  let armorClass = 10 + dex

  if (armor) {
    const heavy = armor.type === '重甲'
    const cap = heavy ? 0 : armor.dexCap
    const dexPart = heavy ? 0 : cap === null || cap === undefined ? dex : Math.min(dex, cap)
    armorClass = armor.baseAc + dexPart + shield
    acSources.push({
      label: armor.name,
      note: `${armor.type}，基础 AC ${armor.baseAc}`,
      value: armor.baseAc,
      plus: false,
    })
    acSources.push({
      label: '敏捷调整值',
      note: heavy ? `${armor.name}是重甲，不计敏捷调整值` : dexNote,
      value: dexPart,
      plus: true,
    })
  } else if (draft.value.classCode === 'monk') {
    armorClass = 10 + dex + wis
    acSources.push(
      { label: '基础 AC', note: '不穿护甲时的起始值', value: 10, plus: false },
      { label: '敏捷调整值', note: dexNote, value: dex, plus: true },
      { label: '感知调整值', note: '武僧「无甲防御」把感知也计入 AC', value: wis, plus: true },
    )
  } else {
    acSources.push(
      { label: '基础 AC', note: '不穿护甲时的起始值', value: 10, plus: false },
      { label: '敏捷调整值', note: dexNote, value: dex, plus: true },
    )
  }

  if (shield > 0) {
    acSources.push({ label: '盾', note: '盾牌 AC +2', value: shield, plus: true })
  }

  items.push({
    key: 'armorClass',
    label: 'AC',
    value: String(armorClass),
    sources: acSources,
  })

  items.push({
    key: 'initiative',
    label: '先攻',
    value: signed(dex),
    sources: [{ label: '敏捷调整值', note: dexNote, value: dex, plus: true }],
  })

  const perceptionProficient = proficientSkillNames.value.has('察觉')
  items.push({
    key: 'passivePerception',
    label: '被动察觉',
    value: String(10 + wis + (perceptionProficient ? 2 : 0)),
    sources: [
      { label: '基础值', note: '被动检定从 10 起算', value: 10, plus: false },
      { label: '感知调整值', note: `感知 ${wisRow?.final ?? 10} → 调整值 ${signed(wis)}`, value: wis, plus: true },
      ...(perceptionProficient
        ? [{ label: '察觉熟练', note: '背景 / 种族给的熟练，加值 +2', value: 2, plus: true }]
        : []),
    ],
  })

  return items
})
const attributeSheetRow = computed<AttributeRow | null>(
  () => attributeRows.value.find((row) => row.key === attributeSheetKey.value) ?? null,
)
const statSheetRow = computed<StatRow | null>(
  () => attributeSnapshot.value.find((row) => row.key === statSheetKey.value) ?? null,
)
/** 选中的阵营：内容还没加载完（旧草稿）时先用草稿里的名字兜底 */
const pickedAlignment = computed<AlignmentOption | null>(() => {
  const code = draft.value.alignmentCode
  if (!code) {
    return null
  }

  return (
    alignments.value.find((item) => item.code === code) ?? {
      code,
      name: draft.value.alignmentName,
    }
  )
})
/** 右侧卡片进度条：职业、种族、亚种都用同一套 */
const railOptions = computed(() => {
  if (isClassStep.value) {
    return classes.value
  }

  if (isRaceStep.value) {
    return races.value
  }

  if (isSubraceStep.value) {
    return subraces.value
  }

  return []
})
const railSelectedCode = computed(() => {
  if (isClassStep.value) {
    return draft.value.classCode
  }

  if (isRaceStep.value) {
    return draft.value.raceCode
  }

  if (isSubraceStep.value) {
    return draft.value.subraceCode
  }

  return null
})
const railSelector = computed(() =>
  isClassStep.value ? '.class-card' : isRaceStep.value ? '.race-card' : '.subrace-card',
)
const isLastStep = computed(() => steps.value.length > 0 && draft.value.step >= steps.value.length)
/** 车卡流程里才显示右下角那组「上一步 / 下一步」 */
const flowActive = computed(() => started.value && steps.value.length > 0)
const nextLabel = computed(() => {
  if (!isLastStep.value) {
    return '下一步'
  }

  return finishing.value ? '提交中…' : '完成'
})
const canNext = computed(() => {
  if (!currentStep.value) {
    return false
  }

  if (isClassStep.value) {
    return Boolean(draft.value.classCode)
  }

  if (isSubclassStep.value) {
    // 列表还在路上时先别放行，免得把「还没加载」当成「没有内容、可以跳过」
    if (subclassesLoading.value && !draft.value.subclassCode) {
      return false
    }

    // 该职业还没配置子职时允许跳过
    return subclasses.value.length === 0 || Boolean(draft.value.subclassCode)
  }

  if (isRaceStep.value) {
    if (racesLoading.value && !draft.value.raceCode) {
      return false
    }

    // 没配种族时允许跳过
    return races.value.length === 0 || Boolean(draft.value.raceCode)
  }

  if (isSubraceStep.value) {
    if (subracesLoading.value && !draft.value.subraceCode) {
      return false
    }

    // 该种族没有亚种（或还没配）时允许跳过
    return subraces.value.length === 0 || Boolean(draft.value.subraceCode)
  }

  if (isBackgroundStep.value) {
    if (alignmentsLoading.value) {
      return false
    }

    const alignmentReady = alignments.value.length === 0 || Boolean(draft.value.alignmentCode)
    const backgroundReady =
      Boolean(draft.value.backgroundCode) &&
      (draft.value.backgroundCode !== CUSTOM_BACKGROUND_CODE || draft.value.customBackground.name.trim().length > 0)
    return alignmentReady && backgroundReady
  }

  if (isAttributesStep.value) {
    return attributesReady()
  }

  if (isSkillsStep.value) {
    return skillsReady.value && featuresReady.value
  }

  if (isEquipmentStep.value) {
    return equipmentReady.value
  }

  return true
})

/** 步骤打勾只看是否真的选过，不看它排在第几个 */
function isStepDone(index: number): boolean {
  const item = steps.value[index]
  if (!item) {
    return false
  }

  switch (item.kind) {
    case 'class':
      return Boolean(draft.value.classCode)
    case 'subclass':
      // 该职业没有子职内容时，选好职业就算完成
      return (
        Boolean(draft.value.subclassCode) ||
        (Boolean(draft.value.classCode) &&
          !subclassesLoading.value &&
          subclassCatalogCode.value === draft.value.classCode &&
          subclasses.value.length === 0)
      )
    case 'race':
      return Boolean(draft.value.raceCode)
    case 'subrace':
      // 该种族没有亚种内容时，选好种族就算完成
      return (
        Boolean(draft.value.subraceCode) ||
        (Boolean(draft.value.raceCode) &&
          !subracesLoading.value &&
          subraceCatalogCode.value === draft.value.raceCode &&
          subraces.value.length === 0)
      )
    case 'background':
      return (
        (Boolean(draft.value.alignmentCode) || (alignmentsLoaded.value && alignments.value.length === 0)) &&
        Boolean(draft.value.backgroundCode) &&
        (draft.value.backgroundCode !== CUSTOM_BACKGROUND_CODE || draft.value.customBackground.name.trim().length > 0)
      )
    case 'attributes':
      return attributesReady()
    case 'skills':
      return skillsReady.value && featuresReady.value
    case 'equipment':
      return equipmentReady.value
    default:
      return false
  }
}

let saveTimer: ReturnType<typeof setTimeout> | null = null

function scheduleSave(): void {
  if (!ready.value) {
    return
  }

  if (saveTimer) {
    clearTimeout(saveTimer)
  }

  saveTimer = setTimeout(() => {
    saveDraft(draft.value)
  }, 250)
}

watch(draft, scheduleSave, { deep: true })

watch(
  () => draft.value.step,
  (value) => {
    const item = steps.value[value - 1]
    // 换步骤后旧的卡片位置全部作废
    railTops.value = []
    activeRailIndex.value = 0
    // 九宫格只在当前步骤里开，切步骤就收起来
    alignmentSheetVisible.value = false
    attributeSheetVisible.value = false
    statSheetVisible.value = false
    // 步骤条跟着当前步骤走，保证它落在一屏三个里
    syncStepperToStep()

    if (item?.kind === 'subclass') {
      void loadSubclasses()
    }

    if (item?.kind === 'class') {
      // 从子职步骤退回时卡片会重新挂载，重新量一次顶部位置
      void nextTick(() => setTimeout(measureRail, 60))
    }

    if (item?.kind === 'race') {
      void loadRaces()
    }

    if (item?.kind === 'subrace') {
      void loadSubraces()
    }

    if (item?.kind === 'background') {
      void loadAlignments()
    }

    if (item?.kind === 'attributes') {
      void ensureAttributeContext()
      // 属性页底部的 AC 要算上职业起始护甲，先把装备包加载好
      void ensureEquipmentData()
    }

    if (item?.kind === 'background') {
      void ensureBackgroundData()
    }

    if (item?.kind === 'skills') {
      void ensureSkillsData()
    }

    if (item?.kind === 'equipment') {
      void ensureEquipmentData()
    }
  },
)

// 步骤条只有进入流程后才渲染，等它出现再量一次宽度
watch(
  [started, () => steps.value.length],
  () => {
    void nextTick(() => measureStepper())
  },
)

onHide(() => {
  if (ready.value) {
    saveDraft(draft.value)
  }
})
onUnload(() => {
  if (ready.value) {
    saveDraft(draft.value)
  }

  if (railIdleTimer) {
    clearTimeout(railIdleTimer)
    railIdleTimer = null
  }

  // #ifdef H5
  window.removeEventListener('resize', measureStepper)
  document.removeEventListener('mousedown', onStepperMouseDown)
  // #endif
})

onLoad(() => {
  void init()
  // #ifdef H5
  window.addEventListener('resize', measureStepper)
  document.addEventListener('mousedown', onStepperMouseDown)
  // #endif
})

onPageScroll((event) => {
  // 一滚就展开右侧索引，停下再慢慢收回去。
  // 切换步骤时页面自己会滚到顶部，那种程序化滚动不算「玩家在滑动」，别把它闪出来
  if (Date.now() >= railWakeMutedUntil) {
    wakeRail()
  }

  // 跳转动画期间不跟着滚动事件改高亮，避免来回闪
  if (jumping.value || railTops.value.length === 0) {
    return
  }

  if (scrollTimer) {
    clearTimeout(scrollTimer)
  }

  scrollTimer = setTimeout(() => {
    const threshold = event.scrollTop + 160
    let index = 0
    railTops.value.forEach((top, position) => {
      if (top <= threshold) {
        index = position
      }
    })
    activeRailIndex.value = index
  }, 60)
})

/** 一滑动就把右侧索引张开，闲置一会儿再让它收回去 */
function wakeRail(): void {
  railOpen.value = true
  if (railIdleTimer) {
    clearTimeout(railIdleTimer)
  }
  railIdleTimer = setTimeout(() => {
    railIdleTimer = null
    railOpen.value = false
  }, RAIL_IDLE_MS)
}

/** 程序化滚动（切步骤 / 点索引跳转）期间不唤醒索引，避免闪一下 */
function muteRailWake(ms: number): void {
  railWakeMutedUntil = Date.now() + ms
}

function measureRail(): void {
  const selector = railSelector.value
  if (!railOptions.value.length) {
    railTops.value = []
    return
  }

  const query = uni.createSelectorQuery()
  query.selectAll(selector).boundingClientRect()
  query.selectViewport().scrollOffset()
  query.exec((result) => {
    const rects = (result?.[0] ?? []) as Array<{ top: number }>
    const scrollTop = (result?.[1]?.scrollTop ?? 0) as number
    railTops.value = rects.map((rect) => rect.top + scrollTop)
  })
}

/* ---------------- 顶部步骤条：3 个一屏 + 左右拖动 ---------------- */

function measureStepper(): void {
  const query = uni.createSelectorQuery()
  query.select('.stepper-viewport').boundingClientRect()
  query.exec((result) => {
    const rect = (result?.[0] ?? null) as { width?: number } | null
    const width = rect?.width ?? 0

    if (width > 0) {
      stepperWidth.value = width
      stepperMeasureRetries = 0
      syncStepperToStep()
      return
    }

    // 刚切到流程页时元素可能还没排版，重试几次
    if (stepperMeasureRetries < 6) {
      stepperMeasureRetries += 1
      setTimeout(measureStepper, 120)
    }
  })
}

/** 当前步骤尽量落在可见的三个里（中间位置优先） */
function syncStepperToStep(): void {
  const width = stepperStepWidth.value
  if (!width) {
    return
  }

  const start = Math.min(Math.max(draft.value.step - 2, 0), stepperMaxStart.value)
  stepperOffset.value = start * width
}

function pointerX(event: { touches?: Array<{ clientX?: number }>; changedTouches?: Array<{ clientX?: number }>; clientX?: number }): number {
  const touch = event?.touches?.[0] ?? event?.changedTouches?.[0]
  return touch?.clientX ?? event?.clientX ?? 0
}

function stepperDragStart(x: number): void {
  if (!stepperStepWidth.value) {
    measureStepper()
  }

  stepperDragging.value = true
  stepperMoved = false
  stepperStartX = x
  stepperBaseOffset = stepperOffset.value
}

function stepperDragMove(x: number): void {
  const width = stepperStepWidth.value
  if (!stepperDragging.value || !width) {
    return
  }

  const delta = x - stepperStartX
  if (Math.abs(delta) > 6) {
    stepperMoved = true
  }

  stepperOffset.value = Math.min(
    Math.max(stepperBaseOffset - delta, 0),
    stepperMaxStart.value * width,
  )
}

function stepperDragEnd(): void {
  if (!stepperDragging.value) {
    return
  }

  stepperDragging.value = false
  const width = stepperStepWidth.value
  if (!width) {
    return
  }

  const start = Math.min(
    Math.max(Math.round(stepperOffset.value / width), 0),
    stepperMaxStart.value,
  )
  stepperOffset.value = start * width

  // 拖动结束后的那次 tap 不要误触步骤跳转
  if (stepperMoved) {
    stepperTapLock = true
    setTimeout(() => {
      stepperTapLock = false
    }, 260)
  }
}

function onStepperTouchStart(event: { touches?: Array<{ clientX?: number }> }): void {
  stepperDragStart(pointerX(event))
}

function onStepperTouchMove(event: { touches?: Array<{ clientX?: number }> }): void {
  stepperDragMove(pointerX(event))
}

function onStepperTouchEnd(event: { changedTouches?: Array<{ clientX?: number }> }): void {
  stepperDragMove(pointerX(event))
  stepperDragEnd()
}

/** H5 桌面端用鼠标拖动；监听挂在 document 上，避免 uni view 的事件绑定差异 */
function onStepperMouseDown(event: MouseEvent): void {
  if (event.button !== 0) {
    return
  }

  const viewport = document.querySelector('.stepper-viewport')
  const target = event.target
  if (!viewport || !(target instanceof Node) || !viewport.contains(target)) {
    return
  }

  stepperDragStart(event.clientX)
  const move = (ev: MouseEvent) => stepperDragMove(ev.clientX)
  const up = () => {
    window.removeEventListener('mousemove', move)
    window.removeEventListener('mouseup', up)
    stepperDragEnd()
  }

  window.addEventListener('mousemove', move)
  window.addEventListener('mouseup', up)
}

function scrollToRail(index: number): void {
  jumping.value = true
  activeRailIndex.value = index

  if (jumpTimer) {
    clearTimeout(jumpTimer)
  }

  jumpTimer = setTimeout(() => {
    jumping.value = false
  }, 420)

  // 每次点击都现量一次，避免图片加载 / 步骤切换后缓存的位置过期
  const query = uni.createSelectorQuery()
  query.selectAll(railSelector.value).boundingClientRect()
  query.selectViewport().scrollOffset()
  query.exec((result) => {
    const rects = (result?.[0] ?? []) as Array<{ top: number }>
    const scrollTop = (result?.[1]?.scrollTop ?? 0) as number
    const rect = rects[index]
    if (!rect) {
      measureRail()
      return
    }

    const isDesktop = uni.getSystemInfoSync().windowWidth >= 900
    muteRailWake(500)
    uni.pageScrollTo({
      scrollTop: Math.max(0, rect.top + scrollTop - (isDesktop ? 96 : 24)),
      duration: 300,
    })
  })
}

async function init(): Promise<void> {
  const restored = loadDraft()
  if (restored) {
    draft.value = restored
  }

  await loadSystems()

  if (draft.value.systemCode && draft.value.systemCode !== 'dnd5e') {
    draft.value.systemCode = null
    draft.value.classCode = null
    draft.value.className = ''
  }

  if (draft.value.systemCode) {
    await loadFlow()
    await loadClasses()
    started.value = true

    if (steps.value.length === 0) {
      draft.value.step = 1
    } else {
      draft.value.step = Math.min(Math.max(1, draft.value.step), steps.value.length)
    }
  } else {
    draft.value.step = 1
  }

  ready.value = true
  void nextTick(() => measureStepper())
  void nextTick(() => measureStepper())

  if (currentStep.value?.kind === 'subclass') {
    void loadSubclasses()
  }

  if (currentStep.value?.kind === 'race') {
    void loadRaces()
  }

  if (currentStep.value?.kind === 'subrace') {
    void loadSubraces()
  }

  if (currentStep.value?.kind === 'background') {
    void loadAlignments()
  }

  if (currentStep.value?.kind === 'attributes') {
    void ensureAttributeContext()
  }

  if (currentStep.value?.kind === 'background') {
    void ensureBackgroundData()
  }

  if (currentStep.value?.kind === 'skills') {
    void ensureSkillsData()
  }

  if (currentStep.value?.kind === 'equipment') {
    void ensureEquipmentData()
  }
}

async function loadSystems(): Promise<void> {
  try {
    systems.value = await request<GameSystem[]>({ url: '/api/systems', auth: false })
  } catch {
    // 错误提示已统一处理
  }
}

async function loadFlow(): Promise<void> {
  if (!draft.value.systemCode) {
    steps.value = []
    return
  }

  flowLoading.value = true
  flowError.value = false

  try {
    const flow = await request<CreationFlow>({
      url: `/api/creation-flow?systemCode=${draft.value.systemCode}`,
      auth: false,
    })
    steps.value = flow.steps ?? []
    await nextTick()
    measureStepper()
  } catch {
    flowError.value = true
    steps.value = []
  } finally {
    flowLoading.value = false
  }
}

async function loadClasses(): Promise<void> {
  classesLoading.value = true
  classesError.value = false

  try {
    const code = draft.value.systemCode ?? 'dnd5e'
    classes.value = await request<ClassOption[]>({
      url: `/api/generator/classes?systemCode=${code}`,
      auth: false,
    })

    await nextTick()
    setTimeout(measureRail, 250)
  } catch {
    classesError.value = true
  } finally {
    classesLoading.value = false
  }
}

/** 职业配图完全来自内容配置（generator.xlsx 的 imageUrl），没配就显示主色块。 */
function coverFor(option: ClassOption): string {
  return (option.imageUrl ?? '').trim()
}

/** 子职配图来自 子职.xlsx 的 imageUrl；没配时显示主色块。 */
function subclassCover(option: SubclassOption): string {
  return (option.imageUrl ?? '').trim()
}

async function loadSubclasses(): Promise<void> {
  if (!draft.value.classCode) {
    subclasses.value = []
    subclassCatalogCode.value = null
    return
  }

  if (subclassesLoading.value) {
    return
  }

  subclassesLoading.value = true
  subclassesError.value = false

  try {
    const code = draft.value.systemCode ?? 'dnd5e'
    const loadedFor = draft.value.classCode
    const list = await request<SubclassOption[]>({
      url: `/api/generator/subclasses?systemCode=${code}&classId=${encodeURIComponent(loadedFor)}`,
      auth: false,
    })
    subclasses.value = list
    subclassCatalogCode.value = loadedFor

    // 恢复的选择如果已经不在内容里，就清掉，避免下一步卡住
    if (
      draft.value.subclassCode &&
      !subclasses.value.some((item) => item.code === draft.value.subclassCode)
    ) {
      draft.value.subclassCode = null
      draft.value.subclassName = ''
    }

    syncClassState()
  } catch {
    subclassesError.value = true
    subclasses.value = []
    subclassCatalogCode.value = null
  } finally {
    subclassesLoading.value = false
  }
}

/** 把当前职业的子职选择存进该职业自己的位置，来回切职业时不丢。 */
function syncClassState(): void {
  const code = draft.value.classCode
  if (!code) {
    return
  }

  draft.value.classStates[code] = {
    subclassCode: draft.value.subclassCode,
    subclassName: draft.value.subclassName,
  }
}

async function loadRaces(): Promise<void> {
  if (racesLoading.value) {
    return
  }

  racesLoading.value = true
  racesError.value = false

  try {
    const code = draft.value.systemCode ?? 'dnd5e'
    races.value = await request<RaceOption[]>({
      url: `/api/generator/races?systemCode=${code}`,
      auth: false,
    })

    // 恢复的选择如果已经不在内容里，就清掉，避免下一步卡住
    if (draft.value.raceCode && !races.value.some((item) => item.code === draft.value.raceCode)) {
      draft.value.raceCode = null
      draft.value.raceName = ''
    }

    await nextTick()
    setTimeout(measureRail, 250)
  } catch {
    racesError.value = true
    races.value = []
  } finally {
    racesLoading.value = false
  }
}

function pickRace(option: RaceOption): void {
  if (draft.value.raceCode !== option.code) {
    // 换种族后，之前选的亚种和自选属性加值都不再适用
    draft.value.subraceCode = null
    draft.value.subraceName = ''
    draft.value.attributeChoices = { ...draft.value.attributeChoices, race: [] }
    subraces.value = []
    subraceCatalogCode.value = null
  }

  draft.value.raceCode = option.code
  draft.value.raceName = option.name
}

async function loadSubraces(): Promise<void> {
  if (!draft.value.raceCode) {
    subraces.value = []
    subraceCatalogCode.value = null
    return
  }

  if (subracesLoading.value) {
    return
  }

  subracesLoading.value = true
  subracesError.value = false

  try {
    const code = draft.value.systemCode ?? 'dnd5e'
    const loadedFor = draft.value.raceCode
    const list = await request<SubraceOption[]>({
      url: `/api/generator/subraces?systemCode=${code}&raceId=${encodeURIComponent(loadedFor)}`,
      auth: false,
    })
    subraces.value = list
    subraceCatalogCode.value = loadedFor

    // 恢复的选择如果已经不在内容里，就清掉，避免下一步卡住
    if (
      draft.value.subraceCode &&
      !subraces.value.some((item) => item.code === draft.value.subraceCode)
    ) {
      draft.value.subraceCode = null
      draft.value.subraceName = ''
    }

    await nextTick()
    setTimeout(measureRail, 250)
  } catch {
    subracesError.value = true
    subraces.value = []
    subraceCatalogCode.value = null
  } finally {
    subracesLoading.value = false
  }
}

function pickSubrace(option: SubraceOption): void {
  draft.value.subraceCode = option.code
  draft.value.subraceName = option.name
}

/* ---------------- 决定背景：阵营九宫格 ---------------- */

async function loadAlignments(): Promise<void> {
  if (alignmentsLoading.value) {
    return
  }

  alignmentsLoading.value = true
  alignmentsError.value = false

  try {
    const code = draft.value.systemCode ?? 'dnd5e'
    alignments.value = await request<AlignmentOption[]>({
      url: `/api/generator/alignments?systemCode=${code}`,
      auth: false,
    })
    alignmentsLoaded.value = true

    // 恢复的选择如果已经不在内容里，就清掉，避免下一步卡住
    if (
      draft.value.alignmentCode &&
      !alignments.value.some((item) => item.code === draft.value.alignmentCode)
    ) {
      draft.value.alignmentCode = null
      draft.value.alignmentName = ''
    }
  } catch {
    alignmentsError.value = true
    alignments.value = []
    alignmentsLoaded.value = false
  } finally {
    alignmentsLoading.value = false
  }
}

function openAlignmentSheet(): void {
  if (!alignments.value.length) {
    void loadAlignments()
    return
  }

  // 九宫格里每格都能点「i」看解释，先把词条拉一遍
  void ensureKeywords()
  alignmentSheetVisible.value = true
}

function closeAlignmentSheet(): void {
  alignmentSheetVisible.value = false
}

function pickAlignment(option: AlignmentOption): void {
  draft.value.alignmentCode = option.code
  draft.value.alignmentName = option.name
  alignmentSheetVisible.value = false
  saveDraft(draft.value)
}

function hasAlignmentKeyword(name: string): boolean {
  return Boolean(findKeyword(name))
}

function openAlignmentKeyword(option: AlignmentOption): void {
  if (!findKeyword(option.name)) {
    return
  }

  openKeyword(option.name)
}

function markAlignmentIconFailed(code: string): void {
  failedAlignmentIcons.value[code] = true
}

/* ---------------- 决定属性 ---------------- */

function signed(value: number): string {
  return value >= 0 ? `+${value}` : String(value)
}

/** 六项都填了、需要自选的加值也选完了，才能进入下一步 */
function attributesReady(): boolean {
  const rows = attributeRows.value
  if (!rows.length || rows.some((row) => row.base <= 0)) {
    return false
  }

  if (draft.value.houseRuleAbilityScores) {
    return Boolean(housePlus2.value && housePlus1.value && housePlus2.value !== housePlus1.value)
  }

  if (raceChoice.value) {
    return chosenRaceChoices.value.length === raceChoice.value.count
  }

  return true
}

/** 投掷：4d6 取高 3 给六个数，直接填进六项；之后怎么改都随你 */
async function rollAttributeValues(): Promise<void> {
  if (attributeRolling.value) {
    return
  }

  attributeRolling.value = true
  attributeError.value = false

  try {
    const code = draft.value.systemCode ?? 'dnd5e'
    const result = await request<AbilityScoresResponse>({
      url: '/api/generator/ability-scores',
      method: 'POST',
      data: { systemCode: code, method: 'roll' },
    })

    const values: Record<string, number> = { ...draft.value.attributeValues }
    for (const item of result.attributes ?? []) {
      values[item.key] = item.base
    }

    draft.value.attributeValues = values
    saveDraft(draft.value)
  } catch {
    attributeError.value = true
    toast('投掷失败，检查后端是否在跑')
  } finally {
    attributeRolling.value = false
  }
}

/** 卡片上直接改初始数值；清空或 0 都算没填 */
function setAttributeValue(key: string, raw: string): void {
  const value = Number(raw)
  const next = { ...draft.value.attributeValues }
  if (!Number.isFinite(value) || value <= 0) {
    delete next[key]
  } else {
    next[key] = Math.min(99, Math.floor(value))
  }

  draft.value.attributeValues = next
}

/** 进入这一步时先把种族 / 亚种内容准备好，属性本身留空等玩家自己填 */
async function ensureAttributeContext(): Promise<void> {
  if (!races.value.length) {
    await loadRaces()
  }

  if (draft.value.raceCode && !subraces.value.length) {
    await loadSubraces()
  }
}

function openAttributeSheet(key: string): void {
  attributeSheetKey.value = key
  attributeSheetVisible.value = true
}

function closeAttributeSheet(): void {
  attributeSheetVisible.value = false
}

function openStatSheet(key: string): void {
  statSheetKey.value = key
  statSheetVisible.value = true
}

function closeStatSheet(): void {
  statSheetVisible.value = false
}

/** 半精灵「任选两项 +1」：点一下选中，再点取消 */
function toggleRaceChoice(key: string): void {
  if (!raceChoice.value) {
    return
  }

  const chosen = [...chosenRaceChoices.value]
  const index = chosen.indexOf(key)
  if (index >= 0) {
    chosen.splice(index, 1)
  } else if (chosen.length < raceChoice.value.count) {
    chosen.push(key)
  } else {
    toast(`最多选 ${raceChoice.value.count} 项，先取消一个`)
    return
  }

  draft.value.attributeChoices = { ...draft.value.attributeChoices, race: chosen }
  saveDraft(draft.value)
}

/** 可选规则：+2 / +1 各挑一项，不能是同一项 */
function pickHouseBonus(slot: 'housePlus2' | 'housePlus1', key: string): void {
  const choices = { ...draft.value.attributeChoices }
  const other = slot === 'housePlus2' ? choices['housePlus1']?.[0] : choices['housePlus2']?.[0]
  if (other === key) {
    toast('+2 和 +1 不能是同一项')
    return
  }

  choices[slot] = choices[slot]?.[0] === key ? [] : [key]
  draft.value.attributeChoices = choices
  saveDraft(draft.value)
}

/** 可选规则：所有种族改用「任意两项 +2 / +1」，替换各族的固定属性加值。 */
const HOUSE_RULE_BONUS = '[属性值加成]：任意两项 +2 / +1'

/* ---------------- 背景详情 / 技能 / 语言 / 职业特性 / 装备 / 描述 ---------------- */

const FEATURE_LABELS: Record<string, string> = {
  fightingStyle: '战斗风格',
  favoredEnemy: '宿敌',
  naturalExplorer: '自然探索者',
}

const skillCatalog = ref<SkillOption[]>([])
const languageCatalog = ref<LanguageOption[]>([])
const backgroundCatalog = ref<BackgroundDetail[]>([])
const featureCatalog = ref<FeatureOption[]>([])
const equipmentCatalog = ref<EquipmentResponse | null>(null)
const backgroundDetailVisible = ref(false)
const backgroundDetailItem = ref<BackgroundDetail | null>(null)

const isSkillsStep = computed(() => currentStep.value?.kind === 'skills')
const isEquipmentStep = computed(() => currentStep.value?.kind === 'equipment')

const classOption = computed<ClassOption | null>(
  () => classes.value.find((item) => item.code === draft.value.classCode) ?? null,
)
const CUSTOM_BACKGROUND_CODE = 'custom'
const backgroundOption = computed<BackgroundDetail | null>(() => {
  const custom = draft.value.customBackground
  if (draft.value.backgroundCode === CUSTOM_BACKGROUND_CODE) {
    return {
      id: CUSTOM_BACKGROUND_CODE,
      name: custom.name || '自定义背景',
      description: custom.description,
      detail: '',
      skills: custom.skills,
      tools: custom.tools,
      languageCount: custom.languageCount,
      equipment: custom.equipment,
      featureName: custom.featureName,
      featureText: custom.featureText,
    }
  }

  return backgroundCatalog.value.find((item) => item.id === draft.value.backgroundCode) ?? null
})

const equipmentKits = computed<EquipmentKit[]>(() => equipmentCatalog.value?.kits ?? [])
/** 选中的起始套装：草稿里记的 kitId 找不到就用第一套 */
const selectedKit = computed<EquipmentKit | null>(() => {
  const kits = equipmentKits.value
  if (!kits.length) {
    return null
  }

  return kits.find((kit) => kit.kitId === draft.value.equipmentKitId) ?? kits[0]
})
const armorOption = computed(() => {
  const kit = selectedKit.value
  if (!kit?.armorId) {
    return null
  }

  return equipmentCatalog.value?.armor.find((item) => item.id === kit.armorId) ?? null
})
const weaponOptions = computed<WeaponOption[]>(() => {
  const kit = selectedKit.value
  if (!kit) {
    return []
  }

  return kit.weaponIds
    .map((id) => equipmentCatalog.value?.weapons.find((weapon) => weapon.id === id))
    .filter((weapon): weapon is WeaponOption => Boolean(weapon))
})
const selectedWeapon = computed<WeaponOption | null>(() => {
  const weapons = weaponOptions.value
  if (!weapons.length) {
    return null
  }

  const id = draft.value.equipmentWeaponId
  const found = id ? weapons.find((weapon) => weapon.id === id) : null
  const fallback =
    weapons.find((weapon) => weapon.id === selectedKit.value?.defaultWeaponId) ?? weapons[0]
  return found ?? fallback
})

/** 套装卡片的副标题：护甲 · 盾 · 武器 */
function kitSummary(kit: EquipmentKit): string {
  const parts: string[] = []
  const armor = equipmentCatalog.value?.armor.find((item) => item.id === kit.armorId)
  parts.push(armor?.name ?? '无甲')
  if (kit.shield) {
    parts.push('盾')
  }

  const weapons = kit.weaponIds
    .map((id) => equipmentCatalog.value?.weapons.find((weapon) => weapon.id === id)?.name)
    .filter(Boolean)
  if (weapons.length) {
    parts.push(weapons.join(' / '))
  }
  if (kit.pack) {
    parts.push(kit.pack)
  }

  return parts.join(' · ')
}

/** 背景 + 种族直接给的技能，不可再选 */
const grantedSkills = computed(
  () => new Set<string>([...(backgroundOption.value?.skills ?? []), ...(raceOption.value?.skills ?? [])]),
)
const skillPickCount = computed(
  () => (classOption.value?.skillChoiceCount ?? 0) + (raceOption.value?.skillChoiceCount ?? 0),
)
/** 技能选项上方的提示：带 [] 的关键词交给 KeywordText 渲染 */
const skillHintText = computed(() => {
  const count = classOption.value?.skillChoiceCount ?? 0
  const race = raceOption.value
  const racePart =
    race && race.skillChoiceCount > 0 ? `，[${race.name}] 再自选 ${race.skillChoiceCount} 项` : ''
  return `[职业]可自选 ${count} 项[技能]${racePart}；[背景]与[种族]固定给的已锁定。`
})
const allowedSkillNames = computed(() => {
  const allowed = new Set(classOption.value?.skillOptions ?? [])
  if ((raceOption.value?.skillChoiceCount ?? 0) > 0) {
    for (const skill of skillCatalog.value) {
      allowed.add(skill.name)
    }
  }

  return allowed
})
const skillRows = computed(() =>
  skillCatalog.value.map((skill) => ({
    ...skill,
    granted: grantedSkills.value.has(skill.name),
    chosen: draft.value.selectedSkills.includes(skill.name),
    selectable: allowedSkillNames.value.has(skill.name),
  })),
)
const skillsReady = computed(() => draft.value.selectedSkills.length >= skillPickCount.value)

const grantedLanguages = computed(() => raceOption.value?.languages ?? [])
const languagePickCount = computed(
  () => (backgroundOption.value?.languageCount ?? 0) + (raceOption.value?.languageChoiceCount ?? 0),
)
const languageRows = computed(() =>
  languageCatalog.value.map((language) => ({
    ...language,
    granted: grantedLanguages.value.includes(language.name),
    chosen: draft.value.selectedLanguages.includes(language.name),
  })),
)

const featureGroups = computed(() => {
  const groups = new Map<string, FeatureOption[]>()
  for (const option of featureCatalog.value) {
    const list = groups.get(option.optionType) ?? []
    list.push(option)
    groups.set(option.optionType, list)
  }

  return [...groups.entries()].map(([type, options]) => ({
    type,
    label: FEATURE_LABELS[type] ?? type,
    options,
  }))
})
const featuresReady = computed(() =>
  featureGroups.value.every((group) => Boolean(draft.value.featureChoices[group.type])),
)

const equipmentReady = computed(() => {
  if (!equipmentCatalog.value) {
    // 还在加载（或加载失败）时先别放行，避免跳到下一步
    return false
  }

  // 没配起始套装的职业直接放行
  return !equipmentKits.value.length || Boolean(selectedWeapon.value)
})

const attributeFinal = computed(() => {
  const map: Record<string, number> = {}
  for (const row of attributeRows.value) {
    map[row.key] = row.final
  }

  return map
})

function modifierOf(key: string): number {
  return Math.floor(((attributeFinal.value[key] ?? 10) - 10) / 2)
}

const proficientSkillNames = computed(
  () => new Set([...grantedSkills.value, ...draft.value.selectedSkills]),
)

const ABILITY_LABELS: Record<string, string> = {
  力量: 'STR',
  敏捷: 'DEX',
  体质: 'CON',
  智力: 'INT',
  感知: 'WIS',
  魅力: 'CHA',
}

/** 车卡里能算出来的战斗数值；装备一改就跟着变 */
const combatSummary = computed(() => {
  const dex = modifierOf('DEX')
  const con = modifierOf('CON')
  const wis = modifierOf('WIS')
  const sides = Number((classOption.value?.hitDie ?? 'd8').replace(/\D/g, '')) || 8
  const armor = armorOption.value
  const shield = selectedKit.value?.shield ? 2 : 0
  let armorClass = 10 + dex

  if (armor) {
    // 重甲不计敏捷调整值，也不会因为敏捷为负而扣 AC
    const heavy = armor.type === '重甲'
    const cap = heavy ? 0 : armor.dexCap
    const dexPart = heavy ? 0 : cap === null || cap === undefined ? dex : Math.min(dex, cap)
    armorClass = armor.baseAc + dexPart + shield
  } else if (draft.value.classCode === 'monk') {
    armorClass = 10 + dex + wis
  } else {
    armorClass = 10 + dex + shield
  }

  const weapon = selectedWeapon.value
  const weaponMod = weapon ? modifierOf(weapon.ability === '力量' ? 'STR' : 'DEX') : 0

  const savingThrows: Record<string, number> = {}
  for (const name of (classOption.value?.savingThrows ?? '').split(',').map((item) => item.trim())) {
    const key = ABILITY_LABELS[name]
    if (key) {
      savingThrows[name] = modifierOf(key) + 2
    }
  }

  return {
    hitPoints: Math.max(1, sides + con),
    armorClass,
    initiative: dex,
    speed: raceOption.value?.speed ?? 30,
    proficiencyBonus: 2,
    passivePerception: 10 + wis + (proficientSkillNames.value.has('察觉') ? 2 : 0),
    savingThrows,
    attackBonus: weapon ? 2 + weaponMod : 0,
    damage: weapon ? `${weapon.damage}${weaponMod >= 0 ? '+' : ''}${weaponMod}` : '',
    weaponName: weapon?.name ?? '',
  }
})

const sheetSkills = computed(() =>
  skillRows.value
    .filter((skill) => skill.granted || skill.chosen)
    .map((skill) => ({
      name: skill.name,
      value: modifierOf(ABILITY_LABELS[skill.ability] ?? 'STR') + 2,
      note: `${skill.ability}熟练`,
    })),
)

const sheetLanguages = computed(() => [...grantedLanguages.value, ...draft.value.selectedLanguages])

async function loadSkillCatalog(): Promise<void> {
  if (skillCatalog.value.length) {
    return
  }

  try {
    const code = draft.value.systemCode ?? 'dnd5e'
    skillCatalog.value = await request<SkillOption[]>({
      url: `/api/generator/skills?systemCode=${code}`,
      auth: false,
    })
  } catch {
    toast('技能加载失败')
  }
}

async function loadLanguageCatalog(): Promise<void> {
  if (languageCatalog.value.length) {
    return
  }

  try {
    const code = draft.value.systemCode ?? 'dnd5e'
    languageCatalog.value = await request<LanguageOption[]>({
      url: `/api/generator/languages?systemCode=${code}`,
      auth: false,
    })
  } catch {
    toast('语言加载失败')
  }
}

async function loadBackgroundCatalog(): Promise<void> {
  if (backgroundCatalog.value.length) {
    return
  }

  try {
    const code = draft.value.systemCode ?? 'dnd5e'
    backgroundCatalog.value = await request<BackgroundDetail[]>({
      url: `/api/generator/backgrounds?systemCode=${code}`,
      auth: false,
    })
  } catch {
    toast('背景加载失败')
  }
}

async function loadFeatureCatalog(): Promise<void> {
  if (!draft.value.classCode) {
    featureCatalog.value = []
    return
  }

  try {
    const code = draft.value.systemCode ?? 'dnd5e'
    featureCatalog.value = await request<FeatureOption[]>({
      url: `/api/generator/class-options?systemCode=${code}&classId=${encodeURIComponent(draft.value.classCode)}`,
      auth: false,
    })
  } catch {
    featureCatalog.value = []
  }
}

async function loadEquipmentCatalog(): Promise<void> {
  if (!draft.value.classCode) {
    equipmentCatalog.value = null
    return
  }

  try {
    const code = draft.value.systemCode ?? 'dnd5e'
    equipmentCatalog.value = await request<EquipmentResponse>({
      url: `/api/generator/equipment?systemCode=${code}&classId=${encodeURIComponent(draft.value.classCode)}`,
      auth: false,
    })
  } catch {
    equipmentCatalog.value = null
  }
}

async function ensureBackgroundData(): Promise<void> {
  if (!races.value.length) {
    await loadRaces()
  }

  await loadBackgroundCatalog()
  await loadSkillCatalog()
  await loadLanguageCatalog()
}

async function ensureSkillsData(): Promise<void> {
  await ensureBackgroundData()
  await loadSkillCatalog()
  await loadFeatureCatalog()
}

async function ensureLanguagesData(): Promise<void> {
  await ensureBackgroundData()
  await loadLanguageCatalog()
}

async function ensureFeaturesData(): Promise<void> {
  await loadFeatureCatalog()
}

async function ensureEquipmentData(): Promise<void> {
  await loadEquipmentCatalog()
}

function pickBackground(item: BackgroundDetail): void {
  if (draft.value.backgroundCode === item.id) {
    return
  }

  draft.value.backgroundCode = item.id
  draft.value.backgroundName = item.id === CUSTOM_BACKGROUND_CODE
    ? draft.value.customBackground.name || '自定义背景'
    : item.name
  draft.value.selectedSkills = []
  draft.value.selectedLanguages = []
  saveDraft(draft.value)
}

function openBackgroundDetail(item: BackgroundDetail): void {
  backgroundDetailItem.value = item
  backgroundDetailVisible.value = true
}

function closeBackgroundDetail(): void {
  backgroundDetailVisible.value = false
}

function setCustomBackgroundText(
  key: 'name' | 'description' | 'tools' | 'equipment' | 'featureName' | 'featureText',
  value: string,
): void {
  draft.value.customBackground = { ...draft.value.customBackground, [key]: value }
  if (key === 'name') {
    draft.value.backgroundName = value || '自定义背景'
  }

  saveDraft(draft.value)
}

function setCustomLanguageCount(value: string): void {
  const count = Number(value)
  draft.value.customBackground = {
    ...draft.value.customBackground,
    languageCount: Number.isFinite(count) ? Math.min(5, Math.max(0, Math.floor(count))) : 0,
  }
  saveDraft(draft.value)
}

function toggleCustomSkill(name: string): void {
  const skills = [...draft.value.customBackground.skills]
  const index = skills.indexOf(name)
  if (index >= 0) {
    skills.splice(index, 1)
  } else {
    skills.push(name)
  }

  draft.value.customBackground = { ...draft.value.customBackground, skills }
  saveDraft(draft.value)
}

function pickSkill(name: string): void {
  const row = skillRows.value.find((item) => item.name === name)
  if (!row || row.granted || !row.selectable) {
    return
  }

  const selected = [...draft.value.selectedSkills]
  const index = selected.indexOf(name)
  if (index >= 0) {
    selected.splice(index, 1)
  } else if (selected.length < skillPickCount.value) {
    selected.push(name)
  } else {
    toast(`最多选 ${skillPickCount.value} 项技能`)
    return
  }

  draft.value.selectedSkills = selected
}

function pickLanguage(name: string): void {
  const row = languageRows.value.find((item) => item.name === name)
  if (!row || row.granted) {
    return
  }

  if (languagePickCount.value <= 0) {
    return
  }

  const selected = [...draft.value.selectedLanguages]
  const index = selected.indexOf(name)
  if (index >= 0) {
    selected.splice(index, 1)
  } else if (selected.length < languagePickCount.value) {
    selected.push(name)
  } else {
    toast(`最多再选 ${languagePickCount.value} 门语言`)
    return
  }

  draft.value.selectedLanguages = selected
}

function pickFeature(type: string, name: string): void {
  draft.value.featureChoices = { ...draft.value.featureChoices, [type]: name }
}

function pickKit(kitId: string): void {
  if (draft.value.equipmentKitId === kitId) {
    return
  }

  draft.value.equipmentKitId = kitId
  // 换套装后武器跟着换回这套的默认武器
  draft.value.equipmentWeaponId = null
}

function pickWeapon(id: string): void {
  draft.value.equipmentWeaponId = id
}

function setDraftText(
  key: 'characterName' | 'gender' | 'height' | 'weight' | 'appearance' | 'trait' | 'ideal' | 'bond' | 'flaw',
  value: string,
): void {
  draft.value[key] = value
}

function setDraftAge(value: string): void {
  const age = Number(value)
  draft.value.age = Number.isFinite(age) && age > 0 ? Math.floor(age) : null
}

async function randomCharacterName(): Promise<void> {
  try {
    const names = await request<string[]>({
      url: `/api/generator/names?systemCode=${draft.value.systemCode}&count=1`,
      auth: false,
    })
    if (names[0]) {
      draft.value.characterName = names[0]
    }
  } catch {
    toast('名字生成失败')
  }
}

function abilityText(option: RaceOption): string {
  return draft.value.houseRuleAbilityScores ? HOUSE_RULE_BONUS : option.abilityBonus
}

/** 开启可选规则后，卡面上的固定「属性值加成」那一条不再适用，收起来。 */
function visibleTraits(option: RaceOption): string[] {
  if (!draft.value.houseRuleAbilityScores) {
    return option.traits
  }

  return option.traits.filter((trait) => !trait.startsWith('[属性值加成]'))
}

function toggleHouseRule(): void {
  draft.value.houseRuleAbilityScores = !draft.value.houseRuleAbilityScores
  saveDraft(draft.value)
}

function markIconFailed(code: string): void {
  failedIcons.value[code] = true
}

function pickSystem(system: GameSystem): void {
  if (!system.available) {
    return
  }

  if (draft.value.systemCode !== system.code) {
    draft.value.systemCode = system.code
    draft.value.classCode = null
    draft.value.className = ''
    draft.value.subclassCode = null
    draft.value.subclassName = ''
    draft.value.raceCode = null
    draft.value.raceName = ''
    draft.value.subraceCode = null
    draft.value.subraceName = ''
    draft.value.alignmentCode = null
    draft.value.alignmentName = ''
    draft.value.backgroundCode = null
    draft.value.backgroundName = ''
    draft.value.attributeValues = {}
    draft.value.attributeChoices = {}
    draft.value.selectedSkills = []
    draft.value.selectedLanguages = []
    draft.value.featureChoices = {}
    draft.value.equipmentKitId = null
    draft.value.equipmentWeaponId = null
    draft.value.classStates = {}
    classes.value = []
    subclasses.value = []
    races.value = []
    subraces.value = []
    alignments.value = []
    alignmentsLoaded.value = false
    alignmentSheetVisible.value = false
    subclassCatalogCode.value = null
    subraceCatalogCode.value = null
    failedCovers.value = {}
    failedSubclassCovers.value = {}
    failedAlignmentIcons.value = {}
    backgroundCatalog.value = []
    featureCatalog.value = []
    equipmentCatalog.value = null
    void loadFlow()
    void loadClasses()
  }
}

async function startFlow(): Promise<void> {
  if (!draft.value.systemCode) {
    toast('先选规则书')
    return
  }

  if (flowLoading.value) {
    return
  }

  await loadFlow()
  if (steps.value.length === 0) {
    toast('这个规则书还没配置车卡流程')
    return
  }

  if (!classes.value.length) {
    void loadClasses()
  }

  started.value = true
  draft.value.step = 1
  saveDraft(draft.value)
  scrollTop()
  void nextTick(() => measureStepper())
}

function pickClass(option: ClassOption): void {
  if (draft.value.classCode === option.code) {
    return
  }

  // 先把当前职业的所有选择存起来，切回来时还能恢复
  syncClassState()

  const saved = draft.value.classStates[option.code]
  draft.value.classCode = option.code
  draft.value.className = option.name
  draft.value.subclassCode = saved?.subclassCode ?? null
  draft.value.subclassName = saved?.subclassName ?? ''
  draft.value.selectedSkills = []
  draft.value.featureChoices = {}
  draft.value.equipmentKitId = null
  draft.value.equipmentWeaponId = null
  featureCatalog.value = []
  equipmentCatalog.value = null
  subclasses.value = []
  subclassCatalogCode.value = null
}

function pickSubclass(option: SubclassOption): void {
  draft.value.subclassCode = option.code
  draft.value.subclassName = option.name
  syncClassState()
}

/** 点 stepper 跳到任意步骤；只有子职依赖职业、亚种依赖种族。 */
function jumpToStep(index: number): void {
  if (stepperTapLock) {
    return
  }

  const target = index + 1
  if (target === draft.value.step) {
    return
  }

  const item = steps.value[index]
  if (item?.kind === 'subclass' && !draft.value.classCode) {
    toast('请先选择职业')
    return
  }

  if (item?.kind === 'subrace' && !draft.value.raceCode) {
    toast('请先选择种族')
    return
  }

  // 决定背景依赖职业和种族；决定属性依赖决定背景（阵营）
  if (item?.kind === 'background' && (!draft.value.classCode || !draft.value.raceCode)) {
    toast('请先选择职业和种族')
    return
  }

  if (item?.kind === 'attributes' && !draft.value.alignmentCode) {
    toast('请先决定阵营')
    return
  }

  if (item?.kind === 'skills' && !draft.value.backgroundCode) {
    toast('请先选择背景')
    return
  }

  if (item?.kind === 'equipment' && !draft.value.classCode) {
    toast('请先选择职业')
    return
  }

  draft.value.step = target
  saveDraft(draft.value)
  scrollTop()
}

function openDetail(option: ClassOption): void {
  detailClass.value = option
  detailVisible.value = true
}

function next(): void {
  if (!canNext.value) {
    toast(
      isClassStep.value
        ? '先选一个职业'
        : isSubclassStep.value
          ? '先选一个子职'
          : isRaceStep.value
            ? '先选一个种族'
            : isSubraceStep.value
              ? '先选一个亚种'
              : isBackgroundStep.value
                ? '先从九宫格里选一个阵营'
                : isAttributesStep.value
                  ? '先把六项属性定下来'
                  : isSkillsStep.value
                    ? '先选好技能，再把职业特性选掉'
                    : '这一步还不能继续',
    )
    return
  }

  if (isLastStep.value) {
    void finish()
    return
  }

  draft.value.step = Math.min(step.value + 1, steps.value.length)
  saveDraft(draft.value)
  scrollTop()
}

function prev(): void {
  if (step.value > 1) {
    draft.value.step = step.value - 1
    saveDraft(draft.value)
    scrollTop()
    return
  }

  // 第一步再往前 = 回到选规则书（不算步骤），职业选择先留着
  started.value = false
  draft.value.step = 1
  saveDraft(draft.value)
  scrollTop()
}

async function finish(): Promise<void> {
  if (!draft.value.classCode) {
    toast('先选一个职业')
    return
  }

  finishing.value = true
  try {
    let name = draft.value.characterName.trim()
    if (!name) {
      try {
        const names = await request<string[]>({
          url: `/api/generator/names?systemCode=${draft.value.systemCode}&count=1`,
          auth: false,
        })
        name = names[0] ?? ''
      } catch {
        // 名字生成失败就用占位名
      }
    }

    const equipmentItems: Array<{ name: string; quantity: number; note?: string }> = []
    const kit = selectedKit.value
    if (armorOption.value) {
      equipmentItems.push({ name: armorOption.value.name, quantity: 1, note: armorOption.value.type })
    }

    if (kit?.shield) {
      equipmentItems.push({ name: '盾', quantity: 1, note: 'AC +2' })
    }

    // 套装里的武器全部带上，选中的那把标成主手
    for (const weapon of weaponOptions.value) {
      const main = weapon.id === selectedWeapon.value?.id
      equipmentItems.push({
        name: weapon.name,
        quantity: 1,
        note: `${main ? '主手' : '备用'} · ${weapon.damage} ${weapon.damageType}`,
      })
    }

    if (kit?.pack) {
      equipmentItems.push({ name: kit.pack, quantity: 1 })
    }

    const backgroundEquipment = (backgroundOption.value?.equipment ?? '')
      .split(/[、,，]/)
      .map((item) => item.trim())
      .filter((item) => item && item !== '无')
    for (const item of backgroundEquipment) {
      equipmentItems.push({ name: item, quantity: 1, note: '背景装备' })
    }

    const created = await request<{ id: string }>({
      url: '/api/characters',
      method: 'POST',
      data: {
        systemCode: draft.value.systemCode,
        name: name || '未命名冒险者',
        className: draft.value.className,
        subclassName: draft.value.subclassName || null,
        raceName: draft.value.raceName || null,
        subraceName: draft.value.subraceName || null,
        alignmentName: draft.value.alignmentName || null,
        sheet: {
          attributes: attributeRows.value
            .filter((row) => row.base > 0)
            .map((row) => ({
              key: row.key,
              label: row.label,
              value: row.final,
              formula: row.formula,
            })),
          skills: sheetSkills.value,
          items: [],
          background: draft.value.backgroundName || null,
          backgroundName: draft.value.backgroundName || null,
          backgroundFeatureName: backgroundOption.value?.featureName || null,
          backgroundFeatureText: backgroundOption.value?.featureText || null,
          gender: draft.value.gender || null,
          height: draft.value.height || null,
          weight: draft.value.weight || null,
          appearance: draft.value.appearance || null,
          languages: sheetLanguages.value,
          proficiencies: {
            armor: classOption.value?.armorProficiencies ?? [],
            weapons: classOption.value?.weaponProficiencies ?? [],
            tools: (backgroundOption.value?.tools ?? '')
              .split(/[、,，]/)
              .map((item) => item.trim())
              .filter((item) => item && item !== '无'),
          },
          equipment: equipmentItems,
          combat: {
            hitPoints: combatSummary.value.hitPoints,
            armorClass: combatSummary.value.armorClass,
            initiative: combatSummary.value.initiative,
            speed: combatSummary.value.speed,
            proficiencyBonus: combatSummary.value.proficiencyBonus,
            passivePerception: combatSummary.value.passivePerception,
            savingThrows: combatSummary.value.savingThrows,
          },
          personality: {
            trait: draft.value.trait || null,
            ideal: draft.value.ideal || null,
            bond: draft.value.bond || null,
            flaw: draft.value.flaw || null,
          },
          featureChoices: draft.value.featureChoices,
        },
      },
    })

    clearDraft()
    uni.redirectTo({ url: `/pages/detail/detail?id=${created.id}` })
  } catch {
    // 提交失败时保留本地草稿
  } finally {
    finishing.value = false
  }
}

function toast(title: string): void {
  uni.showToast({ title, icon: 'none' })
}

function scrollTop(): void {
  muteRailWake(500)
  uni.pageScrollTo({ scrollTop: 0, duration: 200 })
}
</script>

<style lang="scss" scoped>
.hero {
  padding: 8rpx 0 32rpx;
  text-align: center;
}

.hero-en {
  font-family: 'Songti SC', 'SimSun', 'STSong', 'Noto Serif SC', serif;
  font-size: 64rpx;
  font-weight: 600;
  letter-spacing: 8rpx;
  line-height: 1.2;
}

.hero-zh {
  margin-top: 8rpx;
  font-family: 'Songti SC', 'SimSun', 'STSong', 'Noto Serif SC', serif;
  font-size: 24rpx;
  letter-spacing: 8rpx;
  color: $trpg-muted;
}

.stepper-viewport {
  overflow: hidden;
  margin-bottom: 28rpx;
  user-select: none;
  -webkit-user-select: none;
}

.stepper {
  display: flex;
}

.step {
  position: relative;
  flex: 0 0 calc(100% / 3);
  display: flex;
  flex-direction: column;
  align-items: center;
}

.step::after {
  content: '';
  position: absolute;
  top: 26rpx;
  left: 50%;
  z-index: 0;
  width: 100%;
  height: 2rpx;
  background: $trpg-line;
}

.step:last-child::after {
  display: none;
}

.step-dot {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 52rpx;
  height: 52rpx;
  border-radius: 50%;
  border: 2rpx solid $trpg-line;
  background: #fffdf8;
  color: $trpg-muted;
  font-size: 24rpx;
}

.step-done .step-dot {
  border-color: #d8c6a5;
  background: #e7dbc4;
  color: $trpg-accent-deep;
}

.step-active .step-dot {
  border-color: $trpg-accent;
  background: $trpg-accent;
  color: #fffaf0;
}

.step-label {
  margin-top: 8rpx;
  font-size: 20rpx;
  color: $trpg-muted;
}

.step-active .step-label {
  color: $trpg-ink;
  font-weight: 600;
}

/* 已完成的步骤可以点回去 */
.step-clickable {
  cursor: pointer;
}

.step-clickable .step-label {
  color: $trpg-accent-deep;
  text-decoration: underline dotted;
  text-underline-offset: 6rpx;
}

.step-clickable:active {
  opacity: 0.7;
}

.panel {
  padding: 28rpx;
  margin-bottom: 24rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 20rpx;
  background: #fffdf8;
}

.panel-title {
  margin-bottom: 20rpx;
  font-size: 30rpx;
  font-weight: 600;
}

.rulebook-hint {
  margin-bottom: 20rpx;
  font-size: 22rpx;
  line-height: 1.5;
}

.rulebook-actions {
  margin-top: 24rpx;
}

.step-hint {
  margin: -8rpx 0 20rpx;
  font-size: 22rpx;
}

.placeholder-panel {
  display: flex;
  flex-direction: column;
  gap: 12rpx;
}

.placeholder-kind {
  font-size: 20rpx;
}

.system-grid {
  display: flex;
  flex-direction: column;
  gap: 20rpx;
}

.system-card {
  position: relative;
  display: flex;
  gap: 20rpx;
  padding: 20rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 16rpx;
  background: #fff;
}

.system-selected {
  border-color: $trpg-accent;
  box-shadow: 0 8rpx 24rpx rgba(91, 70, 54, 0.12);
}

.system-locked {
  opacity: 0.55;
}

.system-icon-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 112rpx;
  height: 112rpx;
  border-radius: 14rpx;
  flex-shrink: 0;
}

.system-icon {
  width: 84rpx;
  height: 84rpx;
}

.system-icon-fallback {
  color: #fffaf0;
  font-size: 48rpx;
}

.system-info {
  flex: 1;
  min-width: 0;
}

.system-name {
  font-size: 28rpx;
  font-weight: 600;
}

.system-desc {
  margin-top: 6rpx;
  font-size: 22rpx;
}

.system-lock {
  position: absolute;
  top: 16rpx;
  right: 16rpx;
  font-size: 20rpx;
}

.system-check {
  position: absolute;
  top: 16rpx;
  right: 16rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40rpx;
  height: 40rpx;
  border-radius: 50%;
  background: $trpg-accent;
  color: #fffaf0;
  font-size: 22rpx;
}

.load-error {
  padding: 20rpx 0;
}

.class-list {
  display: flex;
  flex-direction: column;
  gap: 24rpx;
}

.class-rail {
  position: fixed;
  top: 50%;
  right: 12px;
  z-index: 60;
  display: flex;
  flex-direction: column;
  gap: 4rpx;
  padding: 10rpx;
  border: 2rpx solid $trpg-line;
  /* 不再是满圆角胶囊：两端弧线会把贴近边缘的文字切掉 */
  border-radius: 18px;
  background: rgba(255, 253, 248, 0.94);
  box-shadow: 0 5px 15px rgba(91, 70, 54, 0.14);
  transform: translateY(-50%);
  /* 展开快一点、收回去慢一点，两头都有动画 */
  transition: transform 0.22s ease-out, background-color 0.22s ease, box-shadow 0.22s ease;
  /* 条目多 / 窗口矮时最多长到这里，给右下角按钮留位 */
  max-height: calc(100vh - 160px);
  overflow-y: auto;
}

/* 玩家停下滑动：索引滑到右边只剩一条细线，不挡着读卡片 */
.class-rail.rail-idle {
  transform: translate(calc(100% + 6px), -50%);
  background: rgba(122, 92, 62, 0.6);
  box-shadow: none;
  transition-duration: 0.5s;
  transition-timing-function: ease-in-out;
}

/* 进度条自己就是操作区，不能再长出一条滚动条 */
/* #ifdef H5 */
.class-rail {
  scrollbar-width: none;
  -ms-overflow-style: none;
}

.class-rail::-webkit-scrollbar {
  width: 0;
  height: 0;
  display: none;
}
/* #endif */

.rail-item {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 2rpx;
  min-width: 44rpx;
  min-height: 44rpx;
  padding: 0 10rpx;
  border-radius: 8rpx;
  color: $trpg-muted;
}

.rail-item.active {
  color: $trpg-ink;
}

.rail-name {
  font-size: 18rpx;
  transition: font-size 0.15s ease;
}

.rail-item.active .rail-name {
  font-size: 26rpx;
  font-weight: 600;
}

.rail-check {
  font-size: 14rpx;
  color: $trpg-accent;
}

.class-card {
  position: relative;
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  border: 2rpx solid $trpg-line;
  border-radius: 20rpx;
  background: #fff;
}

.class-ribbon {
  position: absolute;
  right: -72rpx;
  bottom: 34rpx;
  z-index: 3;
  width: 230rpx;
  padding: 8rpx 0;
  background: $trpg-accent;
  color: #fffaf0;
  font-size: 20rpx;
  letter-spacing: 4rpx;
  text-align: center;
  transform: rotate(-45deg);
}

.class-cover-wrap {
  position: relative;
  width: 100%;
  height: 0;
  padding-top: 56.25%;
  overflow: hidden;
  background: #2b211a;
}

.class-cover {
  position: absolute;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  display: block;
  width: 100%;
  height: 100%;
}

.class-cover-fallback {
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fffaf0;
  font-size: 64rpx;
}

.class-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12rpx;
}

.class-detail {
  flex-shrink: 0;
  padding: 4rpx 16rpx;
  border: 2rpx solid #d8c6a5;
  border-radius: 999rpx;
  font-size: 20rpx;
  color: $trpg-accent-deep;
}

.class-detail:active {
  opacity: 0.7;
}

.class-name {
  font-size: 30rpx;
  font-weight: 600;
  letter-spacing: 2rpx;
}

.class-sub {
  margin-top: 4rpx;
  font-size: 20rpx;
  color: $trpg-muted;
}

.class-body {
  flex: 1;
  padding: 20rpx 20rpx 56rpx;
}

.class-desc {
  margin-top: 12rpx;
  font-size: 22rpx;
  line-height: 1.5;
  color: #4a4238;
}

.feature-list {
  display: grid;
  grid-template-columns: 1fr 1fr;
  column-gap: 24rpx;
  row-gap: 8rpx;
  margin-top: 12rpx;
}

.feature {
  display: flex;
  gap: 8rpx;
  font-size: 20rpx;
  line-height: 1.5;
  color: $trpg-muted;
}

.feature-dot {
  color: $trpg-accent;
}

.subclass-list {
  display: flex;
  flex-direction: column;
  gap: 24rpx;
}

.subclass-card {
  position: relative;
  overflow: hidden;
  border: 2rpx solid $trpg-line;
  border-radius: 20rpx;
  background: #fff;
}

.subclass-cover-wrap {
  position: relative;
  width: 100%;
  height: 0;
  padding-top: 56.25%;
  overflow: hidden;
  background: #2b211a;
}

.subclass-cover {
  position: absolute;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  display: block;
  width: 100%;
  height: 100%;
}

.subclass-cover-fallback {
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fffaf0;
  font-size: 64rpx;
}

.subclass-body {
  padding: 20rpx;
}

.subclass-name {
  font-size: 30rpx;
  font-weight: 600;
  letter-spacing: 2rpx;
}

.subclass-desc {
  margin-top: 8rpx;
  font-size: 22rpx;
  line-height: 1.5;
  color: #4a4238;
}

.subclass-features {
  grid-template-columns: 1fr;
}

.subclass-empty {
  line-height: 1.6;
}

.race-list {
  display: flex;
  flex-direction: column;
  gap: 20rpx;
}

.race-card {
  padding: 20rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 16rpx;
  background: #fff;
}

.race-selected {
  border-color: $trpg-accent;
  box-shadow: 0 8rpx 24rpx rgba(91, 70, 54, 0.12);
}

/* 选中的卡片统一高亮边框 */
.class-selected,
.subclass-selected,
.race-selected,
.subrace-selected {
  border-color: $trpg-accent;
  box-shadow: 0 8rpx 26rpx rgba(91, 70, 54, 0.18);
}

.race-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12rpx;
}

.race-features {
  grid-template-columns: 1fr;
  margin-top: 10rpx;
}

.subrace-list {
  display: flex;
  flex-direction: column;
  gap: 20rpx;
}

.subrace-card {
  padding: 20rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 16rpx;
  background: #fff;
}

.subrace-selected {
  border-color: $trpg-accent;
  box-shadow: 0 8rpx 24rpx rgba(91, 70, 54, 0.12);
}

.subrace-empty {
  line-height: 1.6;
}

/* ---------------- 决定背景：阵营那一栏 + 九宫格 ---------------- */

.background-empty {
  line-height: 1.6;
}

.background-fields {
  display: flex;
  flex-direction: column;
  gap: 16rpx;
}

.bg-field {
  display: flex;
  align-items: center;
  gap: 16rpx;
  padding: 22rpx 24rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 16rpx;
  background: #fff;
}

.bg-field-filled {
  border-color: $trpg-accent;
  background: #fffdf8;
  box-shadow: 0 8rpx 24rpx rgba(91, 70, 54, 0.12);
}

.bg-field:active {
  opacity: 0.85;
}

.bg-field-body {
  flex: 1;
  min-width: 0;
}

.bg-field-label {
  font-size: 22rpx;
  letter-spacing: 2rpx;
  color: $trpg-muted;
}

.bg-field-value {
  display: flex;
  align-items: center;
  gap: 12rpx;
  margin-top: 8rpx;
}

.bg-field-icon {
  flex-shrink: 0;
  width: 56rpx;
  height: 56rpx;
}

.bg-field-icon-fallback {
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 12rpx;
  color: #fffaf0;
  font-size: 28rpx;
}

.bg-field-name {
  font-size: 30rpx;
  font-weight: 600;
  letter-spacing: 2rpx;
}

.bg-field-arrow {
  flex-shrink: 0;
  font-size: 34rpx;
  line-height: 1;
  color: $trpg-muted;
}

.bg-field-tip {
  font-size: 20rpx;
  line-height: 1.6;
}

.align-mask {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  z-index: 200;
  display: flex;
  align-items: flex-end;
  background: rgba(30, 23, 18, 0.5);
  animation: align-fade 0.16s ease-out;
}

.align-sheet {
  box-sizing: border-box;
  width: 100%;
  max-height: 86vh;
  padding: 28rpx 24rpx;
  padding-bottom: calc(32rpx + constant(safe-area-inset-bottom));
  padding-bottom: calc(32rpx + env(safe-area-inset-bottom));
  overflow-y: auto;
  border-radius: 26rpx 26rpx 0 0;
  background: #fffdf8;
  animation: align-rise 0.22s ease-out;
}

.align-sheet-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.align-sheet-title {
  font-size: 32rpx;
  font-weight: 600;
  letter-spacing: 2rpx;
}

.align-sheet-close {
  padding: 0 8rpx;
  font-size: 40rpx;
  line-height: 1;
  color: $trpg-muted;
}

.align-sheet-hint {
  margin: 8rpx 0 22rpx;
  font-size: 20rpx;
  line-height: 1.6;
}

.align-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 14rpx;
}

.align-cell {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8rpx;
  padding: 26rpx 8rpx 16rpx;
  border: 2rpx solid $trpg-line;
  border-top-width: 8rpx;
  border-radius: 14rpx;
  background: #fff;
}

.align-cell-selected {
  border-color: $trpg-accent;
  border-top-color: $trpg-accent !important;
  background: #f7efe1;
  box-shadow: 0 8rpx 24rpx rgba(91, 70, 54, 0.16);
}

.align-cell:active {
  opacity: 0.85;
}

.align-icon {
  width: 64rpx;
  height: 64rpx;
}

.align-icon-fallback {
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 12rpx;
  color: #fffaf0;
  font-size: 30rpx;
}

.align-name {
  font-size: 22rpx;
  letter-spacing: 1rpx;
  color: $trpg-ink;
  text-align: center;
}

.align-info {
  position: absolute;
  top: 8rpx;
  right: 8rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 30rpx;
  height: 30rpx;
  border: 2rpx solid #d8c6a5;
  border-radius: 50%;
  background: #fffdf8;
  font-size: 18rpx;
  font-style: italic;
  color: $trpg-muted;
}

.align-check {
  position: absolute;
  top: 8rpx;
  left: 10rpx;
  font-size: 20rpx;
  color: $trpg-accent;
}

@keyframes align-fade {
  from {
    opacity: 0;
  }

  to {
    opacity: 1;
  }
}

@keyframes align-rise {
  from {
    opacity: 0.6;
    transform: translateY(40rpx);
  }

  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* ---------------- 决定属性：六项属性卡 + 来源弹窗 ---------------- */

.attr-head {
  display: flex;
  align-items: center;
  justify-content: flex-start;
  gap: 16rpx;
  margin-bottom: 20rpx;
}

.attr-head-title {
  margin-bottom: 0;
}

.attr-roll {
  flex: none;
  height: 64rpx;
  padding: 0 28rpx;
  margin-left: auto;
  margin-right: 0;
  font-size: 24rpx;
  line-height: 64rpx;
}

.attr-rolling {
  padding: 10rpx 0;
}

.attr-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16rpx;
}

.attr-card {
  position: relative;
  padding: 18rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 16rpx;
  background: #fff;
}

.attr-card-ready {
  border-color: #d8c6a5;
}

.attr-card-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8rpx;
}

.attr-name {
  font-size: 24rpx;
  letter-spacing: 2rpx;
}

.attr-input-wrap {
  margin-top: 8rpx;
}

.attr-input {
  box-sizing: border-box;
  width: 100%;
  height: 96rpx;
  padding: 0 16rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 12rpx;
  background: #fffdf8;
  font-size: 58rpx;
  font-weight: 600;
  color: $trpg-accent-deep;
}

.attr-choice {
  padding: 18rpx;
  margin-top: 20rpx;
  border: 2rpx dashed #d8c6a5;
  border-radius: 14rpx;
  background: #fff;
}

.attr-choice-title {
  font-size: 24rpx;
  font-weight: 600;
}

.attr-choice-hint {
  margin-top: 4rpx;
  font-size: 20rpx;
  line-height: 1.5;
}

.attr-choice-row {
  display: flex;
  align-items: flex-start;
  gap: 10rpx;
  margin-top: 12rpx;
}

.attr-chip-label {
  flex: none;
  width: 44rpx;
  font-size: 22rpx;
  color: $trpg-muted;
}

.attr-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 10rpx;
  margin-top: 12rpx;
}

.attr-choice-row .attr-chips {
  margin-top: 0;
}

.attr-chip {
  padding: 8rpx 16rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 999rpx;
  font-size: 22rpx;
  background: #fffdf8;
}

.attr-chip-on {
  border-color: $trpg-accent;
  background: $trpg-accent;
  color: #fffaf0;
}

.attr-snapshot {
  display: flex;
  flex-wrap: wrap;
  padding: 16rpx 0 4rpx;
  margin-top: 20rpx;
  border-top: 2rpx dashed $trpg-line;
}

.attr-snap {
  width: 25%;
  text-align: center;
}

.attr-snap-value {
  font-size: 34rpx;
  font-weight: 600;
  color: $trpg-accent-deep;
}

.attr-snap-label {
  font-size: 20rpx;
}

.attr-snap-link {
  margin-top: 2rpx;
  font-size: 18rpx;
  color: $trpg-accent;
}

/* 最终属性：初始值叠加种族 / 亚种加值后的结果 */
.attr-final {
  padding: 18rpx;
  margin-top: 20rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 16rpx;
  background: #fffdf8;
}

.attr-final-head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 12rpx;
  margin-bottom: 12rpx;
}

.attr-final-title {
  flex: none;
  font-size: 24rpx;
  font-weight: 600;
  letter-spacing: 2rpx;
}

.attr-final-hint {
  flex: 1;
  font-size: 20rpx;
  text-align: right;
}

.attr-final-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 10rpx;
}

.attr-final-cell {
  padding: 10rpx 6rpx;
  border-radius: 12rpx;
  background: #fff;
  text-align: center;
}

.attr-final-value {
  font-size: 34rpx;
  font-weight: 600;
  line-height: 1.2;
  color: $trpg-accent-deep;
}

.attr-final-empty .attr-final-value {
  color: #c9bca8;
}

.attr-final-label {
  font-size: 20rpx;
}

.attr-mask {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  z-index: 210;
  display: flex;
  align-items: flex-end;
  background: rgba(30, 23, 18, 0.5);
  animation: align-fade 0.16s ease-out;
}

.attr-sheet {
  box-sizing: border-box;
  width: 100%;
  max-height: 86vh;
  padding: 28rpx 24rpx;
  padding-bottom: calc(32rpx + constant(safe-area-inset-bottom));
  padding-bottom: calc(32rpx + env(safe-area-inset-bottom));
  overflow-y: auto;
  border-radius: 26rpx 26rpx 0 0;
  background: #fffdf8;
  animation: align-rise 0.22s ease-out;
}

.attr-sheet-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.attr-sheet-title {
  font-size: 32rpx;
  font-weight: 600;
  letter-spacing: 2rpx;
}

.attr-sheet-close {
  padding: 0 8rpx;
  font-size: 40rpx;
  line-height: 1;
  color: $trpg-muted;
}

.attr-sheet-total {
  display: flex;
  align-items: baseline;
  gap: 12rpx;
  margin: 10rpx 0 18rpx;
}

.attr-sheet-number {
  font-size: 76rpx;
  font-weight: 600;
  line-height: 1;
  color: $trpg-accent-deep;
}

.attr-sheet-mod {
  font-size: 22rpx;
}

.attr-sources {
  border-top: 2rpx solid $trpg-line;
}

.attr-source {
  padding: 14rpx 0;
  border-bottom: 2rpx solid $trpg-line;
}

.attr-source-main {
  display: flex;
  justify-content: space-between;
  gap: 16rpx;
  font-size: 24rpx;
}

.attr-source-label {
  color: $trpg-ink;
}

.attr-source-value {
  font-weight: 600;
  color: $trpg-accent-deep;
}

.attr-source-plus {
  color: $trpg-accent;
}

.attr-source-note {
  margin-top: 4rpx;
  font-size: 20rpx;
  line-height: 1.5;
}

.attr-sheet-done {
  margin-top: 24rpx;
}

/* ---------------- 背景 / 技能 / 语言 / 特性 / 装备 / 描述 ---------------- */

.bg-section-title {
  margin: 24rpx 0 12rpx;
  font-size: 24rpx;
  font-weight: 600;
  letter-spacing: 2rpx;
}

.bg-block {
  padding: 20rpx;
  margin-top: 20rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 16rpx;
  background: #fffdf8;
}

.bg-block:first-of-type {
  margin-top: 0;
}

.bg-block-title {
  margin-bottom: 14rpx;
  font-size: 26rpx;
  font-weight: 600;
  letter-spacing: 2rpx;
}

.bg-cards {
  display: flex;
  flex-direction: column;
  gap: 14rpx;
}

.bg-card {
  padding: 18rpx 20rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 14rpx;
  background: #fff;
}

.bg-card-selected {
  border-color: $trpg-accent;
  background: #fffdf8;
  box-shadow: 0 8rpx 24rpx rgba(91, 70, 54, 0.12);
}

.bg-card-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.bg-card-name {
  font-size: 26rpx;
  font-weight: 600;
}

.bg-card-desc {
  margin-top: 6rpx;
  font-size: 22rpx;
  line-height: 1.5;
}

.bg-card-meta {
  margin-top: 4rpx;
  font-size: 20rpx;
}

.bg-card-actions {
  display: flex;
  align-items: center;
  gap: 12rpx;
}

.bg-card-detail {
  padding: 2rpx 12rpx;
  border: 2rpx solid #d8c6a5;
  border-radius: 999rpx;
  font-size: 20rpx;
  color: $trpg-accent-deep;
}

.bg-card-custom {
  border-style: dashed;
}

.custom-bg {
  padding: 4rpx 16rpx;
  margin-top: 16rpx;
  border: 2rpx dashed #d8c6a5;
  border-radius: 12rpx;
  background: #fff;
}

.custom-skills {
  flex-direction: column;
  align-items: flex-start;
}

.custom-skills .skill-grid {
  margin-top: 8rpx;
}

.bg-detail-desc {
  margin: 4rpx 0 14rpx;
  font-size: 22rpx;
  line-height: 1.6;
}

.bg-detail-long {
  padding: 16rpx 18rpx;
  margin: 10rpx 0 14rpx;
  border-left: 4rpx solid $trpg-accent;
  border-radius: 8rpx;
  background: #fff;
}

.bg-detail-long-title {
  margin-bottom: 8rpx;
  font-size: 22rpx;
  font-weight: 600;
  letter-spacing: 2rpx;
  color: $trpg-accent-deep;
}

.bg-detail-long-text {
  font-size: 24rpx;
  line-height: 1.8;
  text-align: justify;
  color: $trpg-ink;
}

.bg-grant {
  padding: 16rpx 18rpx;
  margin-top: 14rpx;
  border: 2rpx dashed #d8c6a5;
  border-radius: 12rpx;
  background: #fffdf8;
}

.bg-grant-row {
  display: flex;
  gap: 14rpx;
  font-size: 22rpx;
  line-height: 1.7;
}

.bg-grant-row text:first-child {
  flex: none;
  width: 110rpx;
}

.option-hint {
  margin-bottom: 14rpx;
  font-size: 20rpx;
  line-height: 1.6;
}

.option-count {
  margin-top: 14rpx;
  font-size: 20rpx;
}

.skill-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 12rpx;
}

.skill-chip {
  display: flex;
  align-items: baseline;
  gap: 8rpx;
  padding: 10rpx 18rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 999rpx;
  background: #fff;
  font-size: 22rpx;
}

.skill-chip .skill-ability {
  font-size: 18rpx;
}

.skill-granted {
  border-color: #d8c6a5;
  background: #f3e7d4;
  color: $trpg-accent-deep;
}

.skill-chosen {
  border-color: $trpg-accent;
  background: $trpg-accent;
  color: #fffaf0;
}

.skill-locked {
  opacity: 0.4;
}

.feature-group {
  margin-top: 18rpx;
}

.feature-block {
  padding-top: 20rpx;
  margin-top: 24rpx;
  border-top: 2rpx dashed $trpg-line;
}

.equip-kits {
  display: flex;
  flex-wrap: wrap;
  gap: 14rpx;
  margin-bottom: 18rpx;
}

.equip-kit-card {
  box-sizing: border-box;
  width: 100%;
  padding: 16rpx 18rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 14rpx;
  background: #fff;
  transition: border-color 0.15s ease, background 0.15s ease;
}

.equip-kit-on {
  border-color: $trpg-accent;
  background: #fdf8ef;
}

.equip-kit-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12rpx;
}

.equip-kit-name {
  font-size: 24rpx;
  font-weight: 600;
  letter-spacing: 1rpx;
}

.equip-kit-line {
  margin-top: 6rpx;
  font-size: 20rpx;
  line-height: 1.5;
}

.equip-kit {
  padding: 16rpx 18rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 12rpx;
  background: #fff;
}

.equip-row {
  display: flex;
  gap: 14rpx;
  font-size: 22rpx;
  line-height: 1.7;
}

.equip-row text:first-child {
  flex: none;
  width: 110rpx;
}

.equip-stats {
  display: flex;
  padding-top: 16rpx;
  margin-top: 20rpx;
  border-top: 2rpx dashed $trpg-line;
}

.equip-stat {
  display: flex;
  flex: 1;
  flex-direction: column;
  align-items: center;
  gap: 4rpx;
  font-size: 20rpx;
  text-align: center;
}

.equip-stat-value {
  font-size: 34rpx;
  font-weight: 600;
  color: $trpg-accent-deep;
}

.desc-field {
  display: flex;
  align-items: flex-start;
  gap: 14rpx;
  padding: 14rpx 0;
  border-bottom: 2rpx solid $trpg-line;
}

.desc-field:last-child {
  border-bottom: none;
}

.desc-label {
  flex: none;
  width: 110rpx;
  font-size: 22rpx;
  line-height: 56rpx;
}

.desc-input {
  flex: 1;
  height: 56rpx;
  font-size: 24rpx;
}

.desc-textarea {
  flex: 1;
  width: 100%;
  min-height: 140rpx;
  font-size: 24rpx;
  line-height: 1.6;
}

.desc-name-row {
  display: flex;
  flex: 1;
  align-items: center;
  gap: 10rpx;
}

.desc-random {
  flex: none;
  padding: 6rpx 16rpx;
  border: 2rpx solid #d8c6a5;
  border-radius: 999rpx;
  font-size: 20rpx;
  color: $trpg-accent-deep;
}

.race-house-rule {
  display: flex;
  align-items: center;
  gap: 16rpx;
  padding: 16rpx 18rpx;
  margin-bottom: 20rpx;
  border: 2rpx dashed #d8c6a5;
  border-radius: 14rpx;
  background: #fff;
}

.race-house-rule-on {
  border-style: solid;
  border-color: $trpg-accent;
  background: #f7efe1;
}

.house-rule-body {
  flex: 1;
  min-width: 0;
}

.house-rule-title {
  font-size: 24rpx;
  font-weight: 600;
}

.house-rule-hint {
  margin-top: 4rpx;
  font-size: 20rpx;
  line-height: 1.5;
}

.house-rule-switch {
  flex-shrink: 0;
  width: 76rpx;
  height: 40rpx;
  padding: 4rpx;
  border-radius: 999rpx;
  background: #e4dccd;
}

.house-rule-switch.on {
  background: $trpg-accent;
}

.house-rule-knob {
  width: 32rpx;
  height: 32rpx;
  border-radius: 50%;
  background: #fffdf8;
  transition: transform 0.15s ease;
}

.house-rule-switch.on .house-rule-knob {
  transform: translateX(36rpx);
}

.race-name {
  font-size: 30rpx;
  font-weight: 600;
  letter-spacing: 2rpx;
}

.race-check {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40rpx;
  height: 40rpx;
  border-radius: 50%;
  background: $trpg-accent;
  color: #fffaf0;
  font-size: 22rpx;
}

.race-meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12rpx;
  margin-top: 8rpx;
  font-size: 20rpx;
  color: $trpg-muted;
}

.race-badge {
  padding: 2rpx 12rpx;
  border: 2rpx solid #d8c6a5;
  border-radius: 999rpx;
  color: $trpg-accent-deep;
}

.race-desc {
  margin-top: 10rpx;
  font-size: 22rpx;
  line-height: 1.5;
  color: #4a4238;
}

.race-empty {
  line-height: 1.6;
}

.btn[disabled] {
  opacity: 0.4;
}

/* 车卡操作固定在右下角：种族 / 亚种再长也不用滚到底。
   和右侧进度条刻意分开——进度条是垂直居中的半透明细胶囊（纯展示 + 快速跳转），
   这里是贴着页面底边的实心按钮；进度条另有最大高度，永远压不到这组按钮上 */
.wizard-dock {
  position: fixed;
  right: 16px;
  bottom: 16px;
  bottom: calc(16px + constant(safe-area-inset-bottom));
  bottom: calc(16px + env(safe-area-inset-bottom));
  z-index: 70;
  display: flex;
  align-items: center;
  gap: 10px;
}

.wizard-dock .btn {
  flex: none;
  height: 44px;
  padding: 0 18px;
  font-size: 15px;
  box-shadow: 0 6px 16px rgba(43, 33, 26, 0.2);
  transition: opacity 0.12s ease;
}

/* 「上一步」是次要操作，给个实底才好压住背后的卡片 */
.wizard-dock .btn-plain {
  background: #fffdf8;
}

/* 禁用态沿用同一套配色，只压暗。
   uni 默认会把没有 type 的禁用按钮刷成浅灰底 + 黑字，和启用态的棕底来回切很扎眼 */
.wizard-dock .btn[disabled] {
  background: $trpg-accent;
  color: #fffaf0;
  opacity: 0.42;
}

.wizard-dock .btn-plain[disabled] {
  background: #fffdf8;
  color: $trpg-accent;
  border-color: $trpg-accent;
}

/* 自己接管按下反馈：uni 默认的 button-hover 是浅灰底，会把实心棕按钮闪成灰色 */
.btn-pressed {
  opacity: 0.82;
}

.wizard-dock-next {
  min-width: 96px;
}

/* 底部给按钮留出高度，最后一张卡不会被挡住 */
.wizard-page-docked {
  padding-bottom: calc(88px + constant(safe-area-inset-bottom));
  padding-bottom: calc(88px + env(safe-area-inset-bottom));
}

@media (min-width: 900px) {
  .page {
    max-width: 860px;
  }

  .hero {
    padding: 4px 0 26px;
  }

  .hero-en {
    font-size: 44px;
    letter-spacing: 10px;
  }

  .hero-zh {
    margin-top: 6px;
    font-size: 13px;
    letter-spacing: 8px;
  }

  .stepper-viewport {
    margin-bottom: 24px;
    cursor: grab;
  }

  .stepper-viewport:active {
    cursor: grabbing;
  }

  .step::after {
    top: 14px;
    height: 1px;
  }

  .step-dot {
    width: 30px;
    height: 30px;
    border-width: 1px;
    font-size: 13px;
  }

  .step-label {
    margin-top: 6px;
    font-size: 12px;
  }

  .panel {
    padding: 24px;
    border-width: 1px;
    border-radius: 14px;
    margin-bottom: 20px;
  }

  .panel-title {
    margin-bottom: 16px;
    font-size: 17px;
  }

  .rulebook-hint,
  .step-hint {
    font-size: 13px;
  }

  .system-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px;
  }

  .system-card {
    padding: 16px;
    border-width: 1px;
    border-radius: 12px;
    cursor: pointer;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
  }

  .system-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 10px 26px rgba(91, 70, 54, 0.1);
  }

  .system-icon-wrap {
    width: 72px;
    height: 72px;
    border-radius: 10px;
  }

  .system-icon {
    width: 56px;
    height: 56px;
  }

  .system-name {
    font-size: 16px;
  }

  .system-desc {
    font-size: 13px;
  }

  .system-check {
    top: 12px;
    right: 12px;
    width: 22px;
    height: 22px;
    font-size: 12px;
  }

  .system-lock {
    top: 12px;
    right: 12px;
    font-size: 11px;
  }

  .class-list {
    gap: 18px;
  }

  .subclass-list {
    gap: 18px;
  }

  .subclass-card {
    border-width: 1px;
    border-radius: 14px;
    cursor: pointer;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
  }

  .subclass-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 12px 30px rgba(91, 70, 54, 0.14);
  }

  .subclass-body {
    padding: 16px;
  }

  .subclass-name {
    font-size: 22px;
    letter-spacing: 1px;
  }

  .subclass-desc {
    margin-top: 8px;
    font-size: 13px;
  }

  .race-list {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 14px;
  }

  .subrace-list {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 14px;
  }

  .subrace-card {
    padding: 16px;
    border-width: 1px;
    border-radius: 12px;
    cursor: pointer;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
  }

  .subrace-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 10px 26px rgba(91, 70, 54, 0.1);
  }

  .bg-field {
    padding: 14px 16px;
    border-width: 1px;
    border-radius: 12px;
    cursor: pointer;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
  }

  .bg-field:hover {
    transform: translateY(-1px);
    box-shadow: 0 10px 26px rgba(91, 70, 54, 0.1);
  }

  .bg-field-label {
    font-size: 12px;
  }

  .bg-field-icon {
    width: 34px;
    height: 34px;
  }

  .bg-field-name {
    font-size: 18px;
    letter-spacing: 1px;
  }

  .bg-field-arrow {
    font-size: 22px;
  }

  .bg-field-tip {
    font-size: 12px;
  }

  .align-mask {
    align-items: center;
    justify-content: center;
    padding: 24px;
  }

  .align-sheet {
    width: 480px;
    max-height: 80vh;
    padding: 22px 20px;
    border-radius: 16px;
  }

  .align-sheet-title {
    font-size: 18px;
  }

  .align-sheet-hint {
    font-size: 12px;
    margin: 6px 0 18px;
  }

  .align-grid {
    gap: 10px;
  }

  .align-cell {
    padding: 16px 8px 12px;
    border-width: 1px;
    border-top-width: 5px;
    border-radius: 10px;
    cursor: pointer;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
  }

  .align-cell:hover {
    transform: translateY(-2px);
    box-shadow: 0 10px 24px rgba(91, 70, 54, 0.12);
  }

  .align-icon {
    width: 36px;
    height: 36px;
  }

  .align-name {
    font-size: 13px;
  }

  .align-info {
    top: 5px;
    right: 5px;
    width: 18px;
    height: 18px;
    border-width: 1px;
    font-size: 11px;
  }

  .align-check {
    top: 5px;
    left: 6px;
    font-size: 11px;
  }

  .attr-head {
    gap: 12px;
    margin-bottom: 16px;
  }

  .attr-roll {
    height: 36px;
    padding: 0 18px;
    font-size: 13px;
    line-height: 36px;
  }

  .attr-grid {
    grid-template-columns: repeat(3, 1fr);
    gap: 12px;
  }

  .attr-card {
    padding: 14px;
    border-width: 1px;
    border-radius: 12px;
  }

  .attr-name {
    font-size: 13px;
    letter-spacing: 1px;
  }

  .attr-input {
    height: 52px;
    padding: 0 10px;
    border-width: 1px;
    border-radius: 8px;
    font-size: 34px;
  }

  .attr-choice {
    padding: 14px;
    margin-top: 16px;
    border-width: 1px;
    border-radius: 10px;
  }

  .attr-choice-title {
    font-size: 14px;
  }

  .attr-choice-hint {
    font-size: 12px;
  }

  .attr-chip {
    padding: 5px 12px;
    border-width: 1px;
    font-size: 12px;
    cursor: pointer;
  }

  .attr-snapshot {
    margin-top: 16px;
    padding: 12px 0 2px;
    border-top-width: 1px;
  }

  .attr-snap-value {
    font-size: 20px;
  }

  .attr-snap-label {
    font-size: 12px;
  }

  .attr-snap-link {
    font-size: 11px;
    cursor: pointer;
  }

  .attr-final {
    padding: 14px;
    margin-top: 16px;
    border-width: 1px;
    border-radius: 12px;
  }

  .attr-final-head {
    margin-bottom: 10px;
  }

  .attr-final-title {
    font-size: 14px;
    letter-spacing: 1px;
  }

  .attr-final-hint {
    font-size: 12px;
  }

  .attr-final-grid {
    grid-template-columns: repeat(6, 1fr);
    gap: 8px;
  }

  .attr-final-cell {
    padding: 8px 4px;
    border-radius: 8px;
    cursor: pointer;
    transition: background 0.15s ease;
  }

  .attr-final-cell:hover {
    background: #f7efe1;
  }

  .attr-final-value {
    font-size: 20px;
  }

  .attr-final-label {
    font-size: 12px;
  }

  .attr-mask {
    align-items: center;
    justify-content: center;
    padding: 24px;
  }

  .attr-sheet {
    width: 420px;
    max-height: 80vh;
    padding: 22px 20px;
    border-radius: 16px;
  }

  .attr-sheet-title {
    font-size: 18px;
  }

  .attr-sheet-number {
    font-size: 44px;
  }

  .attr-source {
    padding: 10px 0;
    border-bottom-width: 1px;
  }

  .attr-source-main {
    font-size: 13px;
  }

  .attr-source-note {
    font-size: 12px;
  }

  .bg-section-title {
    margin: 16px 0 8px;
    font-size: 14px;
  }

  .bg-cards {
    gap: 10px;
  }

  .bg-card {
    padding: 14px;
    border-width: 1px;
    border-radius: 10px;
    cursor: pointer;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
  }

  .bg-card:hover {
    transform: translateY(-1px);
    box-shadow: 0 8px 20px rgba(91, 70, 54, 0.1);
  }

  .bg-card-name {
    font-size: 14px;
  }

  .bg-card-desc {
    font-size: 13px;
  }

  .bg-card-meta {
    font-size: 12px;
  }

  .bg-block {
    padding: 16px;
    margin-top: 16px;
    border-width: 1px;
    border-radius: 12px;
  }

  .bg-block-title {
    margin-bottom: 10px;
    font-size: 15px;
  }

  .bg-card-detail {
    padding: 1px 8px;
    border-width: 1px;
    font-size: 12px;
    cursor: pointer;
  }

  .custom-bg {
    padding: 2px 12px;
    margin-top: 12px;
    border-width: 1px;
    border-radius: 10px;
  }

  .bg-detail-desc {
    font-size: 13px;
  }

  .bg-detail-long {
    padding: 12px 14px;
    margin: 8px 0 12px;
    border-left-width: 3px;
  }

  .bg-detail-long-title {
    font-size: 13px;
  }

  .bg-detail-long-text {
    font-size: 14px;
    line-height: 1.8;
  }

  .bg-grant {
    padding: 12px 14px;
    border-width: 1px;
    border-radius: 10px;
  }

  .bg-grant-row {
    font-size: 13px;
  }

  .bg-grant-row text:first-child {
    width: 70px;
  }

  .skill-chip {
    padding: 6px 12px;
    border-width: 1px;
    font-size: 13px;
    cursor: pointer;
  }

  .skill-chip .skill-ability {
    font-size: 11px;
  }

  .feature-group {
    margin-top: 14px;
  }

  .feature-block {
    padding-top: 14px;
    margin-top: 18px;
    border-top-width: 1px;
  }

  .option-hint,
  .option-count {
    font-size: 12px;
  }

  .equip-kits {
    gap: 10px;
    margin-bottom: 14px;
  }

  .equip-kit-card {
    width: calc(50% - 5px);
    padding: 12px 14px;
    border-width: 1px;
    border-radius: 10px;
    cursor: pointer;
  }

  .equip-kit-card:hover {
    border-color: #d8c6a5;
  }

  .equip-kit-name {
    font-size: 14px;
  }

  .equip-kit-line {
    font-size: 12px;
  }

  .equip-kit {
    padding: 12px 14px;
    border-width: 1px;
    border-radius: 10px;
  }

  .equip-row {
    font-size: 13px;
  }

  .equip-row text:first-child {
    width: 70px;
  }

  .equip-stat {
    font-size: 12px;
  }

  .equip-stat-value {
    font-size: 20px;
  }

  .desc-field {
    padding: 10px 0;
    border-bottom-width: 1px;
  }

  .desc-label {
    width: 70px;
    font-size: 12px;
    line-height: 32px;
  }

  .desc-input {
    height: 32px;
    font-size: 13px;
  }

  .desc-textarea {
    min-height: 90px;
    font-size: 13px;
  }

  .desc-random {
    padding: 4px 10px;
    border-width: 1px;
    font-size: 12px;
    cursor: pointer;
  }

  .race-card {
    padding: 16px;
    border-width: 1px;
    border-radius: 12px;
    cursor: pointer;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
  }

  .race-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 10px 26px rgba(91, 70, 54, 0.1);
  }

  .race-name {
    font-size: 18px;
    letter-spacing: 1px;
  }

  .race-meta {
    gap: 8px;
    margin-top: 6px;
    font-size: 12px;
  }

  .race-badge {
    padding: 1px 8px;
    border-width: 1px;
  }

  .race-desc {
    margin-top: 8px;
    font-size: 13px;
  }

  .race-check {
    width: 22px;
    height: 22px;
    font-size: 12px;
  }

  .race-house-rule {
    padding: 12px 14px;
    margin-bottom: 16px;
    border-width: 1px;
    border-radius: 10px;
    cursor: pointer;
  }

  .house-rule-title {
    font-size: 14px;
  }

  .house-rule-hint {
    font-size: 12px;
  }

  .house-rule-switch {
    width: 44px;
    height: 24px;
    padding: 2px;
  }

  .house-rule-knob {
    width: 20px;
    height: 20px;
  }

  .house-rule-switch.on .house-rule-knob {
    transform: translateX(20px);
  }

  .feature-list {
    column-gap: 32px;
  }

  .class-rail {
    right: max(16px, calc((100vw - 860px) / 2 - 68px));
    gap: 6px;
    padding: 10px 8px;
    border-width: 1px;
    border-radius: 20px;
    max-height: calc(100vh - 240px);
  }

  .rail-item {
    min-width: 36px;
    min-height: 36px;
    padding: 0 8px;
    cursor: pointer;
  }

  .rail-item:hover {
    color: $trpg-accent;
  }

  .rail-name {
    font-size: 12px;
  }

  .rail-item.active .rail-name {
    font-size: 16px;
  }

  .rail-check {
    font-size: 10px;
  }

  .class-card {
    border-width: 1px;
    border-radius: 14px;
    cursor: pointer;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
  }

  .class-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 12px 30px rgba(91, 70, 54, 0.14);
  }

  .class-name {
    font-size: 22px;
    letter-spacing: 1px;
  }

  .class-detail {
    padding: 3px 12px;
    border-width: 1px;
    font-size: 12px;
    cursor: pointer;
  }

  .class-sub {
    margin-top: 6px;
    font-size: 12px;
  }

  .class-body {
    padding: 16px 16px 44px;
  }

  .class-ribbon {
    right: -56px;
    bottom: 32px;
    width: 170px;
    padding: 6px 0;
    font-size: 12px;
    letter-spacing: 3px;
  }

  .class-desc {
    margin-top: 10px;
    font-size: 13px;
  }

  .feature {
    font-size: 12px;
  }

  .wizard-dock {
    right: max(16px, calc((100vw - 860px) / 2 + 40px));
    bottom: 32px;
    bottom: calc(32px + constant(safe-area-inset-bottom));
    bottom: calc(32px + env(safe-area-inset-bottom));
    gap: 12px;
  }

  .wizard-dock .btn {
    height: 48px;
    padding: 0 22px;
    font-size: 15px;
    box-shadow: 0 12px 28px rgba(43, 33, 26, 0.24);
  }

  .wizard-dock-next {
    min-width: 104px;
  }

  .wizard-page-docked {
    padding-bottom: calc(128px + constant(safe-area-inset-bottom));
    padding-bottom: calc(128px + env(safe-area-inset-bottom));
  }

  /* 悬停的卡片如果正好是选中项，仍然保持高亮边框 */
  .class-card.class-selected,
  .subclass-card.subclass-selected,
  .race-card.race-selected,
  .subrace-card.subrace-selected {
    border-color: $trpg-accent;
    box-shadow: 0 12px 30px rgba(91, 70, 54, 0.22);
  }
}
</style>
