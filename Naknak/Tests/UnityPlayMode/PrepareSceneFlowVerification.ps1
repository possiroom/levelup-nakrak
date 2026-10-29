$ErrorActionPreference='Stop'
$projectRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixture=Join-Path $projectRoot 'Build/SceneFlowVerification'
New-Item -ItemType Directory -Force -Path "$fixture/Assets/Editor","$fixture/Packages","$fixture/ProjectSettings" | Out-Null
$sources=@('Assets/Scripts/SceneLoadManager.cs','Assets/Scripts/SceneFlowContainer.cs','Assets/Scripts/SceneTransitionPoint.cs','Assets/Scripts/DialogManager.cs','Assets/Scripts/ConditionManager.cs','Assets/Scripts/StorySelectData.cs','Assets/Story/Story Data/StoryData.cs','Assets/Story/Dialog Condition/ConditionID.cs','Assets/Scripts/MenuEventSystemOwner.cs','Assets/Scripts/TutorialScene1Cutscene.cs','Assets/Story/Dialog Container/DialogContainer.cs')
$sources+=@('Assets/Scripts/InventoryItem.cs','Assets/Scripts/InventoryModel.cs','Assets/Scripts/TutorialRoom0.cs','Assets/Scripts/TutorialRoom0Point.cs')
$sources+=@('Assets/Scripts/TutorialT3T4SceneController.cs')
$sources+=@('Assets/Scripts/TutorialT4Controller.cs','Assets/Scripts/MemoryInvestigationPoint.cs')
$sources+=@('Assets/Scripts/TutorialT2Appraisal.cs','Assets/Scripts/NPC.cs')
foreach($source in $sources){Copy-Item -LiteralPath (Join-Path $projectRoot $source) -Destination (Join-Path "$fixture/Assets" ([IO.Path]::GetFileName($source))) -Force}
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets/Story/Dialog Container/DialogContainer.cs.meta') -Destination "$fixture/Assets/DialogContainer.cs.meta" -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets/Story/Dialog Condition/ConditionID.cs.meta') -Destination "$fixture/Assets/ConditionID.cs.meta" -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets/Scripts/InventoryItem.cs.meta') -Destination "$fixture/Assets/InventoryItem.cs.meta" -Force
$t4Dialogue="$fixture/Assets/Resources/T4Dialogue"
$t4Items="$fixture/Assets/Resources/T4Items"
New-Item -ItemType Directory -Force -Path $t4Dialogue,$t4Items | Out-Null
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Story/Tutorial/T4') -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $t4Dialogue $_.Name) -Force }
foreach($name in @('PackedJewelryBox','EmptyJewelryBox','WindingSpring','FlatheadScrew')){
 foreach($suffix in @('.asset','.asset.meta')){Copy-Item -LiteralPath (Join-Path $projectRoot ('Assets/Resources/InventoryItems/'+$name+$suffix)) -Destination (Join-Path $t4Items ($name+$suffix)) -Force}
}
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
$t2Dialogue="$fixture/Assets/Resources/T2Dialogue"
New-Item -ItemType Directory -Force -Path $t2Dialogue | Out-Null
foreach($directory in @('Assets/Scripts/New Folder/Text_T2','Assets/Story/Tutorial/T2')){
 Get-ChildItem -LiteralPath (Join-Path $projectRoot $directory) -File | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $t2Dialogue $_.Name) -Force}
}
foreach($suffix in @('.asset','.asset.meta')){Copy-Item -LiteralPath (Join-Path $projectRoot ('Assets/Scripts/New Folder/Text_T2/Dialog_Container/T2_Owner_Container'+$suffix)) -Destination (Join-Path $t2Dialogue ('T2_Owner_Container'+$suffix)) -Force}
# The long-lived fixture can have a different StoryData script GUID than the main project.
Get-ChildItem -LiteralPath $t4Dialogue,$t2Dialogue -Filter '*.asset' -File | ForEach-Object {
 $text=[IO.File]::ReadAllText($_.FullName).Replace($mainStoryGuid,$fixtureStoryGuid)
 [IO.File]::WriteAllText($_.FullName,$text)
}
$standIns=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'SceneFlowStandIns.cs'))
$replayDialogue="$fixture/Assets/Resources/ReplayDialogue"
New-Item -ItemType Directory -Force -Path $replayDialogue | Out-Null
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Scripts/New Folder/Text_T1') -File -Filter '*.asset*' | ForEach-Object {
 $target=Join-Path $replayDialogue $_.Name
 if($_.Extension -eq '.asset'){[IO.File]::WriteAllText($target,[IO.File]::ReadAllText($_.FullName).Replace($mainStoryGuid,$fixtureStoryGuid))}
 else{Copy-Item -LiteralPath $_.FullName -Destination $target -Force}
}
foreach($suffix in @('.asset','.asset.meta')){Copy-Item -LiteralPath (Join-Path $projectRoot ('Assets/Story/Tutorial/MemoryVisitCount'+$suffix)) -Destination (Join-Path $replayDialogue ('MemoryVisitCount'+$suffix)) -Force}
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
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'T3CutsceneVerification.cs') -Destination "$fixture/Assets/Editor/T3CutsceneVerification.cs" -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'T4MemoryVerification.cs') -Destination "$fixture/Assets/Editor/T4MemoryVerification.cs" -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'T2AppraisalVerification.cs') -Destination "$fixture/Assets/Editor/T2AppraisalVerification.cs" -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'MemoryReplayVerification.cs') -Destination "$fixture/Assets/Editor/MemoryReplayVerification.cs" -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets/Editor/SceneFlowEditor.cs') -Destination "$fixture/Assets/Editor/SceneFlowEditor.cs" -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets/Editor/GamePlayEntry.cs') -Destination "$fixture/Assets/Editor/GamePlayEntry.cs" -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') -Destination "$fixture/ProjectSettings/ProjectVersion.txt" -Force
$ugui=(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Library/PackageCache') -Directory -Filter 'com.unity.ugui@*' | Select-Object -First 1).FullName.Replace('\','/')
if(!$ugui){throw 'Unity UI package cache is required for the EventSystem checks.'}
$manifest=@{dependencies=@{'com.unity.ugui'=('file:'+$ugui);'com.unity.modules.animation'='1.0.0';'com.unity.modules.audio'='1.0.0';'com.unity.modules.physics'='1.0.0';'com.unity.modules.physics2d'='1.0.0';'com.unity.modules.jsonserialize'='1.0.0'}} | ConvertTo-Json -Depth 3
[IO.File]::WriteAllText("$fixture/Packages/manifest.json",$manifest)
Write-Output $fixture
