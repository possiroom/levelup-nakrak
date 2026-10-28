$ErrorActionPreference='Stop'
$projectRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixture=Join-Path $projectRoot 'Build/SceneFlowVerification'
New-Item -ItemType Directory -Force -Path "$fixture/Assets/Editor","$fixture/Packages","$fixture/ProjectSettings" | Out-Null
$sources=@('Assets/Scripts/SceneLoadManager.cs','Assets/Scripts/SceneFlowContainer.cs','Assets/Scripts/SceneTransitionPoint.cs','Assets/Scripts/DialogManager.cs','Assets/Scripts/ConditionManager.cs','Assets/Scripts/StorySelectData.cs','Assets/Story/Story Data/StoryData.cs','Assets/Story/Dialog Condition/ConditionID.cs','Assets/Scripts/MenuEventSystemOwner.cs','Assets/Scripts/TutorialScene1Cutscene.cs','Assets/Story/Dialog Container/DialogContainer.cs')
$sources+=@('Assets/Scripts/InventoryItem.cs','Assets/Scripts/InventoryModel.cs','Assets/Scripts/TutorialRoom0.cs','Assets/Scripts/TutorialRoom0Point.cs')
foreach($source in $sources){Copy-Item -LiteralPath (Join-Path $projectRoot $source) -Destination (Join-Path "$fixture/Assets" ([IO.Path]::GetFileName($source))) -Force}
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets/Story/Dialog Container/DialogContainer.cs.meta') -Destination "$fixture/Assets/DialogContainer.cs.meta" -Force
$dialogueDirectory="$fixture/Assets/S1Dialogue"
New-Item -ItemType Directory -Force -Path $dialogueDirectory | Out-Null
$mainStoryGuid=[regex]::Match([IO.File]::ReadAllText((Join-Path $projectRoot 'Assets/Story/Story Data/StoryData.cs.meta')),'(?m)^guid: (\w+)').Groups[1].Value
$fixtureStoryGuid=$mainStoryGuid
if(Test-Path "$fixture/Assets/StoryData.cs.meta"){$fixtureStoryGuid=[regex]::Match([IO.File]::ReadAllText("$fixture/Assets/StoryData.cs.meta"),'(?m)^guid: (\w+)').Groups[1].Value}
else{Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets/Story/Story Data/StoryData.cs.meta') -Destination "$fixture/Assets/StoryData.cs.meta" -Force}
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Story/Tutorial/S1') -File | ForEach-Object {
 $target=Join-Path $dialogueDirectory $_.Name
 if($_.Extension -eq '.asset'){$text=[IO.File]::ReadAllText($_.FullName).Replace($mainStoryGuid,$fixtureStoryGuid);[IO.File]::WriteAllText($target,$text)}else{Copy-Item -LiteralPath $_.FullName -Destination $target -Force}
}
$standIns=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'SceneFlowStandIns.cs'))
foreach($match in [regex]::Matches($standIns,'public (?:static |sealed )?(?:class|enum|interface) (\w+)')){
 $start=$match.Index
 $brace=$standIns.IndexOf('{',$start)
 $depth=1;$end=$brace+1
 while($depth -gt 0){if($standIns[$end] -eq '{'){$depth++};if($standIns[$end] -eq '}'){$depth--};$end++}
 $body=$standIns.Substring($start,$end-$start)
 [IO.File]::WriteAllText((Join-Path "$fixture/Assets" ($match.Groups[1].Value+'.cs')), "using System.Collections.Generic;`nusing UnityEngine;`n"+$body)
}
# Unity requires a matching source filename for every serialized MonoBehaviour.
foreach($obsolete in @("$fixture/Assets/SceneFlowStandIns.cs","$fixture/Assets/SceneFlowStandIns.cs.meta")){
 if(Test-Path -LiteralPath $obsolete){Remove-Item -LiteralPath $obsolete -Force}
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'SceneFlowVerification.cs') -Destination "$fixture/Assets/Editor/SceneFlowVerification.cs" -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'S1CutsceneVerification.cs') -Destination "$fixture/Assets/Editor/S1CutsceneVerification.cs" -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'T0RoomVerification.cs') -Destination "$fixture/Assets/Editor/T0RoomVerification.cs" -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets/Editor/SceneFlowEditor.cs') -Destination "$fixture/Assets/Editor/SceneFlowEditor.cs" -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets/Editor/GamePlayEntry.cs') -Destination "$fixture/Assets/Editor/GamePlayEntry.cs" -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') -Destination "$fixture/ProjectSettings/ProjectVersion.txt" -Force
$ugui=(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Library/PackageCache') -Directory -Filter 'com.unity.ugui@*' | Select-Object -First 1).FullName.Replace('\','/')
if(!$ugui){throw 'Unity UI package cache is required for the EventSystem checks.'}
$manifest=@{dependencies=@{'com.unity.ugui'=('file:'+$ugui);'com.unity.modules.animation'='1.0.0';'com.unity.modules.audio'='1.0.0';'com.unity.modules.physics'='1.0.0';'com.unity.modules.physics2d'='1.0.0';'com.unity.modules.jsonserialize'='1.0.0'}} | ConvertTo-Json -Depth 3
[IO.File]::WriteAllText("$fixture/Packages/manifest.json",$manifest)
Write-Output $fixture
