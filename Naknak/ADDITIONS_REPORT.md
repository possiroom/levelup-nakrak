# Naknak 추가사항 정리

기준 베이스: `C:\Users\LG\OneDrive\levelup-nakrak\Naknak`

비교 대상:

- `C:\Users\LG\Downloads\naknak-origin1`
- `C:\Users\LG\Downloads\Naknak-main`

정리 기준:

- "추가 파일"은 베이스에는 없고 비교 대상에만 있는 파일입니다.
- 두 비교 대상 모두 베이스와 같은 경로에 있지만 내용이 다른 파일이 1209개 있습니다. 대부분 Unity `.meta`, prefab, asset, project setting 계열까지 포함된 대량 변경이라 여기서는 추가 파일 중심으로 정리했습니다.

## naknak-origin1 추가사항

요약: Alter 추격 몬스터, 실명/시야 제한 오버레이, 오버레이 셰이더/머티리얼, 스태미나/숨고르기/시야 디버프 시스템이 추가되어 있습니다.

추가 파일 수: 19개

### 주요 기능

- `AlterChaserAI.cs`
  - 플레이어를 타일 단위로 추격하는 Alter AI입니다.
  - X/Y 거리 중 더 큰 축을 기준으로 한 칸씩 이동합니다.
  - 거리, Trigger, Collision 방식으로 플레이어 접촉을 감지합니다.
  - 옵션에 따라 접촉 후 Alter 오브젝트를 제거할 수 있습니다.

- `AlterSpawnManager.cs`
  - 실명 디버프 시간을 관리합니다.
  - 디버프 중 일정 간격으로 확률 기반 Alter 스폰을 시도합니다.
  - 스폰 전 오버레이 노이즈로 전조 효과를 켭니다.
  - 플레이어 주변 경계 반경에 Alter를 생성하고 `AlterChaserAI` 추격을 시작합니다.
  - 디버프 종료 시 Alter와 오버레이 상태를 정리합니다.

- `VisionCircleOverlayController.cs`
  - UI Image 머티리얼을 런타임 복사해서 시야 원형 마스크를 제어합니다.
  - 화면 중앙 고정 또는 플레이어 위치 기반 시야 중심을 지원합니다.
  - 시야 반경, 부드러움, 노이즈 강도/속도/스케일/폭을 조절합니다.
  - Show/Hide 및 노이즈 on/off API를 제공합니다.

- `PatienceAngerBreathRunSystem.cs`
  - 플레이어 달리기 속도와 스태미나를 관리합니다.
  - 달리기 중 스태미나를 소모하고, 걷기/정지 상태에서 스태미나를 회복합니다.
  - 스태미나가 부족할 때 UI 경고 글로우를 생성하고 점멸시킵니다.
  - 3페이즈 이상에서 정지 상태가 일정 시간 지속되면 숨고르기 상태로 들어가 시야 디버프 시간을 추가로 줄입니다.
  - 테스트 키로 화재/Alter 조우 시야 디버프를 발생시킬 수 있습니다.
  - 주의: 현재 파일 안의 일부 한글 주석/문자열이 깨져 보이며, `Debug.Log` 문자열 구문은 Unity 컴파일 전에 확인이 필요합니다.

- `VisionCircleOverlay.shader`, `VisionCircleOverlayMat.mat`
  - 시야 제한 오버레이와 노이즈 효과에 쓰이는 셰이더/머티리얼입니다.

- `Alter.prefab`
  - Alter 몬스터 프리팹입니다.

### 추가 파일 목록

- `.vsconfig`
- `Assets\Obstacles_Prefab\Alter.prefab`
- `Assets\Obstacles_Prefab\Alter.prefab.meta`
- `Assets\Scripts\AlterChaserAI.cs`
- `Assets\Scripts\AlterChaserAI.cs.meta`
- `Assets\Scripts\AlterSpawnManager.cs`
- `Assets\Scripts\AlterSpawnManager.cs.meta`
- `Assets\Scripts\AlterSpawnTestInput.cs`
- `Assets\Scripts\AlterSpawnTestInput.cs.meta`
- `Assets\Scripts\PatienceAngerBreathRunSystem`
- `Assets\Scripts\PatienceAngerBreathRunSystem.cs`
- `Assets\Scripts\VisionCircleOverlayController.cs`
- `Assets\Scripts\VisionCircleOverlayController.cs.meta`
- `Assets\VisionCircleOverlay.shader`
- `Assets\VisionCircleOverlay.shader.meta`
- `Assets\VisionCircleOverlayMat.mat`
- `Assets\VisionCircleOverlayMat.mat.meta`
- `Unity 6000.0.57f1.lnk`
- `Unity 6000.0.74f1.lnk`

## Naknak-main 추가사항

요약: 방 그래프 기반 맵 전환, 문 트리거, 적 AI 순찰/추격 시스템이 `Assets\Scripts\New Folder` 아래에 추가되어 있습니다.

추가 파일 수: 40개

### 주요 기능

- `Map_Manager.cs`
  - `RoomData` 배열을 기반으로 방 그래프를 생성합니다.
  - 현재 방 프리팹을 로드하고 기존 방 오브젝트를 제거합니다.
  - 문 방향에 따라 다음 방 ID를 계산해 방을 전환합니다.
  - 플레이어가 방에 들어갔음을 `AIManager`에 전달합니다.

- `RoomGraph.cs`, `RoomNode.cs`, `RoomData.cs`
  - 방 ID와 상하좌우 연결 정보를 그래프로 구성합니다.
  - 각 방의 이웃 방 목록을 조회할 수 있습니다.
  - `RoomData` ScriptableObject로 방 프리팹과 연결 정보를 관리하는 구조입니다.

- `AIPathfinder.cs`
  - BFS로 현재 적 방에서 플레이어 방까지 최단 경로를 찾습니다.

- `AIManager.cs`
  - 적의 현재 방과 플레이어 방을 추적합니다.
  - 플레이어와 같은 방이면 적 프리팹을 생성하고, 다른 방이면 제거합니다.
  - Patrol 상태에서는 인접 방 중 랜덤 이동합니다.
  - Chase 상태에서는 BFS 경로를 따라 플레이어 방 쪽으로 이동합니다.

- `DoorTrigger.cs`, `DoorDirection.cs`, `RoomInstance.cs`
  - 방 프리팹 안의 문 트리거를 초기화합니다.
  - 플레이어가 문에 닿으면 방향에 맞는 다음 방으로 이동시킵니다.

- `EnemyController.cs`, `Enemy.prefab`
  - 방 안에 생성될 적 프리팹과 컨트롤러입니다.

### 추가 파일 목록

- `.vsconfig`
- `Assets\Scripts\New Folder.meta`
- `Assets\Scripts\New Folder\AIManager.cs`
- `Assets\Scripts\New Folder\AIManager.cs.meta`
- `Assets\Scripts\New Folder\AIPathfinder.cs`
- `Assets\Scripts\New Folder\AIPathfinder.cs.meta`
- `Assets\Scripts\New Folder\AIState.cs`
- `Assets\Scripts\New Folder\AIState.cs.meta`
- `Assets\Scripts\New Folder\DoorDirection.cs`
- `Assets\Scripts\New Folder\DoorDirection.cs.meta`
- `Assets\Scripts\New Folder\DoorTrigger.cs`
- `Assets\Scripts\New Folder\DoorTrigger.cs.meta`
- `Assets\Scripts\New Folder\Enemy.prefab`
- `Assets\Scripts\New Folder\Enemy.prefab.meta`
- `Assets\Scripts\New Folder\EnemyController.cs`
- `Assets\Scripts\New Folder\EnemyController.cs.meta`
- `Assets\Scripts\New Folder\Map_Manager.cs`
- `Assets\Scripts\New Folder\Map_Manager.cs.meta`
- `Assets\Scripts\New Folder\Room01.unity`
- `Assets\Scripts\New Folder\Room01.unity.meta`
- `Assets\Scripts\New Folder\Room1.asset`
- `Assets\Scripts\New Folder\Room1.asset.meta`
- `Assets\Scripts\New Folder\Room2.asset`
- `Assets\Scripts\New Folder\Room2.asset.meta`
- `Assets\Scripts\New Folder\Room3.asset`
- `Assets\Scripts\New Folder\Room3.asset.meta`
- `Assets\Scripts\New Folder\RoomData.cs`
- `Assets\Scripts\New Folder\RoomData.cs.meta`
- `Assets\Scripts\New Folder\RoomGraph.cs`
- `Assets\Scripts\New Folder\RoomGraph.cs.meta`
- `Assets\Scripts\New Folder\RoomInstance.cs`
- `Assets\Scripts\New Folder\RoomInstance.cs.meta`
- `Assets\Scripts\New Folder\RoomNode.cs`
- `Assets\Scripts\New Folder\RoomNode.cs.meta`
- `Assets\Scripts\New Folder\Sample Map.prefab`
- `Assets\Scripts\New Folder\Sample Map.prefab.meta`
- `Assets\Scripts\New Folder\Sample Map2.prefab`
- `Assets\Scripts\New Folder\Sample Map2.prefab.meta`
- `Assets\Scripts\New Folder\Straight Game.prefab`
- `Assets\Scripts\New Folder\Straight Game.prefab.meta`

## 참고

- `naknak-origin1`과 `Naknak-main` 모두 베이스 대비 변경 파일이 1209개씩 있습니다.
- 변경 파일 전체에는 `.gitignore`, `.vscode`, `Assets`, `Packages`, `ProjectSettings` 하위의 Unity 에셋/메타/프리팹 변경이 대량 포함되어 있습니다.
- 실제 병합 시에는 위의 "추가 파일"만 옮길지, 기존 파일 1209개 변경까지 반영할지 별도로 결정해야 합니다.
