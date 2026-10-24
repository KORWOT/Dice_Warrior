# V5DieGraphic.cs

- 역할: 실제 v5 물리6면을 uGUI Canvas 안에 투영하는 입체 큐브. 기존 LoopDieGraphic의 면 법선·원근 투영·감속 방식을 v5 면 인덱스에 적용하며 legacy 컴포넌트는 수정하지 않는다.
- 입출력: Configure(V5FaceDefinition[] faces,int resultFaceIndex), Pose(float progress,int slot). ResultFaceIndex/RollProgress/FrontFaceIndex/FaceId(index)는 표시 검증용이며 RevealResult/Held/Pulse는 보기 상태다.6면 배열은 순서와 중복을 보존하며 결과 ID의 첫 일치 위치로 대체하지 않는다.
- 핵심 동작: 확정 resultFaceIndex의 법선을 정면으로 돌린 정지 자세에서 역산한 회전과 제한된 도약을 적용한다. 보이는 면을 ivory body·테두리·같은 V5FaceGraphic.Draw 아이콘으로 투영하고 별도 받침 그림자·청록 잠금을 그린다. RevealResult=false의 정지/첫 프레임은 결과 없는 큐브이며 회전 중에만 실제 구성 면이 움직인다. 공개 시 대표 정면·라벨은 Area가 같은 결과에 맞춘다.
- 사용하는 대상: Unity Quaternion/VertexHelper/MaskableGraphic, V5FaceDefinition, V5FaceGraphic.Draw. 새 카메라/RenderTexture/물리/RNG/저장 경로 없음.
- 사용하는 쪽·관계 근거: V5DiceAreaView가 실제 전투 결과·보관·리롤을 Configure/Pose하고 CompetitionCombatUiAuthoring이3개 authored 큐브를 연결한다. V5ShellView의 주사위 획득 표시는 RevealResult=false의 구성 전시에 재사용하며 실제 결과로 주장하지 않는다. CombatUiTests와 DiceRedesignTests가 투영·결과·복구를 확인한다.
- 상태/수명: 입력6면 배열만 복사하고 콘텐츠 정의는 읽기만 한다. 상태 변경은 SetVerticesDirty로 Canvas 메시를 갱신한다. Graphic과 CanvasRenderer는 Editor authoring이 함께 생성하고 런타임에 생성하지 않는다.
- 검수 주의: `GetMesh()`로 실제 투영된 메시·그림자·잠금까지 검사한다. RectTransform 경계나 마스크만으로 cube 가시성을 단정하지 않는다. Configure는 RevealResult=true/Pulse0/Pose1로 초기화하므로 구성 전시는 호출 후 RevealResult=false로 둔다. 물리 단말 성능/색상은 별도 검증이다.
