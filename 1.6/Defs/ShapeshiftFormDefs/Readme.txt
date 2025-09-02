────────────────────────────
🔻 ShapeshiftFormDef 사용법 / How to use ShapeshiftFormDef 🔻
────────────────────────────

이 폴더에는 RimWorld 변신 모드의 모든 변신폼 정의(XML 파일, *.xml)를 넣습니다.
Put all shapeshift form definition XMLs here.

────────────────────
● 필수/주요 필드 (Required fields)
────────────────────
- defName: 폼의 고유 이름 (중복 불가)  
  (Unique name for the form, required.)
  
- label: 게임 내 표시 이름 (생략 가능, defName으로 대체)  
  (In-game display name, can be omitted: defaults to defName)
  
- description: 설명 (툴팁 등, 생략 가능)  
  (Description for tooltip, can be omitted)

────────────────────
● 외형/그래픽 설정 (Appearance / Graphics)
────────────────────
- body, head, hair, beard, tattooBody, tattooHead:
    → 각 파츠(몸, 머리, 헤어, 수염, 바디/헤드 타투)의 표시 방식을 제어합니다.
    → Control rendering for each part (body, head, hair, beard, body/head tattoo)
    → mode: Default(기본값), Hidden(숨김), Replace(교체)
    (mode 기본값: Default, 즉 아무 것도 입력 안하면 기존 모습 유지)
    (mode default: Default. If omitted, uses vanilla part.)
    → replacementTexPath: 교체용 텍스처 경로(Replace일 때만, 생략시 기존 텍스처 사용)
    (Replacement texture path, only needed for Replace; optional)
    → 남녀(젠더)별로 <male>, <female> 블록을 추가해 각각 다른 그래픽/옵션을 지정할 수 있습니다.
    (If you add <male> and/or <female> blocks inside, you can specify different graphics/options per gender.
    If omitted, common (default) settings are used for all genders.)
	
	예시 / Example:
	<body>
      <mode>Replace</mode>
      <replacementTexPath>Things/Pawn/Beastkin_Body</replacementTexPath>
      <male>
        <mode>Replace</mode>
        <replacementTexPath>Things/Pawn/Beastkin_Body_Male</replacementTexPath>
      </male>
      <female>
        <mode>Replace</mode>
        <replacementTexPath>Things/Pawn/Beastkin_Body_Female</replacementTexPath>
      </female>
    </body>

- customDrawSize: 전체(몸) 스케일 (생략시 1,1=기본)  
  (Overall body scale. Omit for no scaling; default is (1,1))
  
- customHeadDrawSize: 헤드만 별도 스케일 (생략시 1,1)  
  (Head-only scale. Omit for default size)
  
- bodyOffset, headOffset: 각 위치 보정 (생략시 0,0)  
  (Position offset for each part, default (0,0))

────────────────────
● 스탯/능력 변화 (Stat/Ability Effects)
────────────────────
- statOffsets: 변신 시 스탯 더하기(누적)  
  (Additive stat bonuses during transform; optional)
  
- statFactors: 변신 시 스탯 곱하기(배수)  
  (Multiplicative stat factors during transform; optional)

────────────────────
● 변신 조건 (Transform Conditions)
────────────────────
- allowedRaces/disallowedRaces: 허용/불가 종족(pawn.def, 생략시 모두 허용).
  가장 우선적으로 체크되는 조건입니다.
  (Allowed/blocked races by pawn.def. Omit for no restriction. This is the highest priority filter.)
	 
- requiredGenes: 필요 유전자 (생략시 무관)  
  (Required genes. Omit for no requirement)
  
- requiredItems: 필요 인벤토리 아이템 (생략시 무관)
  (Required inventory items. Omit for no requirement.)
  
- requiredApparels: 필요 장비 (생략시 무관)
  (Required apparels. Omit for no requirement.)
  
- requiredAbilities: 필요 능력(Ability) (생략시 무관)
  (Required abilities. Omit for no requirement.)
  
- allowedPreviousForms:
  변신 전에 어떤 상태일 때 변신이 허용되는지 설정합니다.
  특정 변신 상태(defName)이거나 변신하지 않은 상태(None)일 때만 변신할 수 있습니다.
  생략하면 변신하지 않은 상태(무변신)에서만 변신할 수 있습니다.
  (Sets which states allow transformation:
  Only allowed if the pawn is currently in one of the specified forms (by defName), or not transformed ("None").
  If omitted, transformation is only allowed from the untransformed state.)
  
  예시 / Examples:
    allowedPreviousForms 생략시: "아무 변신도 안한 상태"에서만 변신 가능
    (If omitted: Only allowed from untransformed state)
    
    allowedPreviousForms에 None만 있으면: "아무 변신도 안한 상태"에서만 변신 가능
    (If set to None only: Only allowed from untransformed state)
    
    allowedPreviousForms에 FormA만 있으면: FormA 폼 상태에서만 변신 가능
    (If set to FormA only: Only allowed from FormA state)
    
    allowedPreviousForms에 None, FormA 둘 다 있으면: 무변신 상태이거나 FormA 폼 상태에서 변신 가능
    (If set to both None and FormA: Allowed from either untransformed state or FormA state)
  
- conditionMode: All(AND/기본값), Any(OR) 조건 조합.
  종족 제한(allowedRaces/disallowedRaces)으로 먼저 필터링한 후,
  나머지 조건(유전자, 아이템, 능력 등)에 대해 All/Any 조합 적용.
  (All (AND, default) or Any (OR). First, pawn is filtered by allowed/disallowed races; then, remaining conditions are checked using All/Any logic.)

────────────────────
● 부가 옵션 (Additional Options)
────────────────────
- bodyType, headType: 변신 후 바디/헤드 타입 (생략시 기존 유지)  
  (Body/head type after transform; omit for no change)
  
- gizmoIconPath: 변신 버튼 아이콘 경로 (생략시 기본 아이콘)  
  (Gizmo icon; optional, uses default if omitted)
  
- duration: 변신 지속 시간(틱 단위, 1초=60틱, 기본값 무한;
  양수 입력시 해당 시간 동안만 변신, 0 또는 음수/생략/null은 무한 변신)
  (duration in ticks, 60 ticks = 1 sec; default is infinite.
  If set to a positive value, transformation lasts that duration.
  If set to 0, negative, omitted, or null: transformation is infinite.)

- showHarAddons: HAR 바디애드온 렌더 표시 여부 (생략시 false)  
  (Show HAR body addons; default false)
  
- addHediffs: 변신 시 임시로 부여할 Hediff(상태이상, 생략시 없음)  
  (Temporary Hediffs added during transformation)
  
- addAbilities: 변신 시 임시로 부여할 Ability(능력, 생략시 없음)  
  (Temporary Abilities added during transformation)
  
- exclusionTags:  
  - 변신 시 해당 태그(헤어, 수염, 꼬리 등)가 붙은 유전자를 임시로 override(비활성화)합니다.
  - 예: <exclusionTags><li>Hair</li><li>Beard</li></exclusionTags>
	HairStyle, BeardStyle, SkinColorOverride, Fur, EyeColor, Tail, BodyType, Ears, Nose, Jaw, Hands, Headbone, Voice etc...
  - 비워두면(생략/공란) 유전자 오버라이드 없음
  - During transformation, temporarily overrides (disables) genes with these tags (e.g. Hair, Beard, Tail).
  - If omitted, does not override any genes.

- renderNodeProperties: 커스텀 파츠 렌더 속성 (고급옵션, 생략 가능)  
  (Custom render node properties; advanced/optional)

────────────────────
● 옵션별 기본값/생략 안내 (Default Value Guide)
────────────────────
- 대부분의 필드는 입력하지 않으면 **기본값/원본 상태 유지**
- (If you omit an option, it uses the vanilla default or keeps original state.)
- mode = Default, scale/offset = (1,1)/(0,0), duration = 무한/Infinite 등

────────────────────
● exclusionTags 상세 설명 (한글/영어)
────────────────────
- 변신 시 헤어/수염/꼬리 등 특정 태그가 붙은 유전자를 임시로 override(비활성화)합니다.
  예를 들어 exclusionTags에 "Hair"를 넣으면, 해당 폰의 모든 Hair 유전자가 변신 기간 동안 무효화됨.
  변신 해제시 원래대로 복구.
- During transformation, any genes with the listed tags (e.g. Hair, Beard, Tail) are temporarily overridden (disabled).
  For example, if you put "Hair" in exclusionTags, all Hair genes are disabled during the transformation, and restored after.

────────────────────────────
■ 예시 1: 짐승/동물형 폼 (몸만 교체, 나머지 숨김)
■ Example 1: Animal/Beast form (body only, others hidden)
────────────────────────────
<Defs>
  <!--
    [KO] 변신 폼 예시 - 심플 울프폼
    [EN] Example shapeshift form - Simple Wolf
  -->
  <ShapeshiftFormDef>
    <defName>GiantWolfForm</defName> <!-- [필수/required] 폼 고유이름, 중복 불가, 영어 권장 / Unique form name (must be unique) -->
    <label>Giant Wolf</label>        <!-- [생략가능/optional] 게임 내 표시이름, 생략시 defName 사용 / In-game display name, defaults to defName -->
    <description>Become a giant wolf.</description> <!-- [생략가능/optional] 설명 / Description, optional -->
  
    <!-- === 외형/그래픽 설정 (Appearance/Graphics) === -->
	<body>
      <mode>Replace</mode> <!-- [필수/required] Default/Hidden/Replace, 미입력시 Default(기존 바디) / Default is Default -->
      <replacementTexPath>Things/Pawn/Animal/Wolf_Timber/Wolf_Timber</replacementTexPath> <!-- [Replace일 때 필수/required if mode=Replace] -->
    </body>
    <head>
      <mode>Hidden</mode> <!-- [생략가능/optional] 미입력시 Default(기존 머리 유지) / Default: Default (keeps vanilla head) -->
    </head>
    <hair>
      <mode>Hidden</mode>
    </hair>
    <beard>
      <mode>Hidden</mode>
    </beard>
    <tattooBody>
      <mode>Hidden</mode>
    </tattooBody>
    <tattooHead>
      <mode>Hidden</mode>
    </tattooHead>
  
    <!-- === 외형/보정/스케일 (Appearance/Offset/Scale) === -->
    <customDrawSize>(3,3)</customDrawSize> <!-- [생략가능/optional] 전체 스케일, 미입력시 (1,1) / Overall scale, default (1,1) -->

    <!-- === 스탯 변화 (Stat change) === -->
    <statOffsets>
      <li>
        <stat>MoveSpeed</stat>
        <value>1.0</value>
      </li>
    </statOffsets>
    <statFactors>
      <li>
        <stat>MeleeDPS</stat>
        <value>1.3</value>
      </li>
    </statFactors>
  
    <!-- === 변신 부가 효과/필수 조건 (Extras/Conditions) === -->
    <!-- [생략가능/optional] 미입력시 제한 없음 / Omit for no race restriction -->
	<!--
    <allowedRaces>
      <li>Human</li>
    </allowedRaces>
	-->
    <allowedPreviousForms>
      <li>None</li>
      <li>WolfkinForm</li>
      <!-- [생략가능/optional] 미입력시 "변신하지 않은 상태"에서만 변신 가능 / Only allows from untransformed state if omitted -->
    </allowedPreviousForms>
    <conditionMode>All</conditionMode> <!-- All(AND/기본값), Any(OR) 조건 조합 -->
  </ShapeshiftFormDef>
</Defs>
────────────────────────────
■ 예시 2: 인간형 폼 (모든 파츠 커스텀)
■ Example 2: Humanlike form (full custom parts)
────────────────────────────
<Defs>
  <!--
    [KO] 변신 폼 예시 - Wolfkin (남녀별 그래픽 분기 포함)
    [EN] Example shapeshift form - Wolfkin (gender-specific graphics)
  -->
  <ShapeshiftFormDef>
    <defName>WolfkinForm</defName>
    <label>Wolfkin</label>
    <description>A strong and swift wolfkin transformation, with gender-specific appearance.</description>
  
    <!-- === 바디 (Body, gender-specific) === -->
    <body>
      <mode>Replace</mode>
      <replacementTexPath>Things/Pawn/Wolfkin_Body_Common</replacementTexPath> <!-- [공통/Common: 모든 젠더에 적용, 젠더별 없을 때 백업] -->
      <male>
        <mode>Replace</mode>
        <replacementTexPath>Things/Pawn/Wolfkin_Body_Male</replacementTexPath> <!-- [남성/Male only] -->
      </male>
      <female>
        <mode>Replace</mode>
        <replacementTexPath>Things/Pawn/Wolfkin_Body_Female</replacementTexPath> <!-- [여성/Female only] -->
      </female>
    </body>
    <!-- === 머리 (Head, gender-specific) === -->
    <head>
      <mode>Replace</mode>
      <replacementTexPath>Things/Pawn/Wolfkin_Head_Common</replacementTexPath>
      <male>
        <mode>Replace</mode>
        <replacementTexPath>Things/Pawn/Wolfkin_Head_Male</replacementTexPath>
      </male>
      <female>
        <mode>Replace</mode>
        <replacementTexPath>Things/Pawn/Wolfkin_Head_Female</replacementTexPath>
      </female>
    </head>
    <!-- === 헤어/수염, 공통/숨김 (Hair/Beard, common/hidden) === -->
    <hair>
      <mode>Default</mode> <!-- [공통/Common, vanilla hair 유지] -->
    </hair>
    <beard>
      <mode>Hidden</mode> <!-- [공통/Common, 수염 숨김] -->
    </beard>
    <!-- === 타투 (Tattoo, 공통) === -->
    <tattooBody>
      <mode>Default</mode>
    </tattooBody>
    <tattooHead>
      <mode>Default</mode>
    </tattooHead>
  
    <!-- === 외형/보정/스케일 (Appearance/Offset/Scale) === -->
    <customDrawSize>(1.1,1.1)</customDrawSize> <!-- [전체 스케일/Overall scale, 생략가능/optional, default (1,1)] -->
    <customHeadDrawSize>(1.08,1.08)</customHeadDrawSize> <!-- [헤드 전용/Head only, optional, default (1,1)] -->
    <bodyOffset>(0, 0)</bodyOffset> <!-- [생략가능/optional, default (0,0)] -->
    <headOffset>(0, 0.04)</headOffset> <!-- [생략가능/optional, default (0,0)] -->
  
    <bodyType>Male</bodyType> <!-- [생략가능, 공통/optional, default keeps original] -->
    <headType>Male_AverageNormal</headType> <!-- [optional, default keeps original] -->
  
    <!-- === 스탯 변화 (Stat change) === -->
    <statOffsets>
      <li>
        <stat>MoveSpeed</stat>
        <value>0.5</value>
      </li>
      <li>
        <stat>Sight</stat>
        <value>0.1</value>
      </li>
    </statOffsets>
    <statFactors>
      <li>
        <stat>MeleeDPS</stat>
        <value>1.25</value>
      </li>
    </statFactors>
  
    <!-- === 변신 부가 효과/필수 조건 (Extras/Conditions) === -->
    <!-- [생략가능/optional] 미입력시 제한 없음 / Omit for no race restriction -->
	<!--
    <addHediffs>
      <li>FastHealing</li>
    </addHediffs>
    <addAbilities>
      <li>WolfHowl</li>
    </addAbilities>
    <requiredGenes>
      <li>WolfkinGene</li>
    </requiredGenes>
    <allowedRaces>
      <li>Human</li>
    </allowedRaces>
	-->
    <allowedPreviousForms>
      <li>None</li>
      <li>GiantWolfForm</li>
      <!-- [생략가능/optional] 미입력시 "변신하지 않은 상태"에서만 변신 가능 / Only allows from untransformed state if omitted -->
    </allowedPreviousForms>
    <conditionMode>All</conditionMode> <!-- All(AND/기본값), Any(OR) 조건 조합 -->
    <duration>3000</duration> <!-- [생략/0/음수=무한, 3000은 약 50초] -->
    <gizmoIconPath>UI/Commands/WolfHowl</gizmoIconPath>
    <showHarAddons>false</showHarAddons>
  </ShapeshiftFormDef>
</Defs>
