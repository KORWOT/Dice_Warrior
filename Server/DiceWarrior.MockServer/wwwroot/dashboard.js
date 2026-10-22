"use strict";
const $=id=>document.getElementById(id);
const number=value=>Number(value).toLocaleString("ko-KR",{maximumFractionDigits:2});
const bytes=value=>value>=1073741824?number(value/1073741824)+" GiB":value>=1048576?number(value/1048576)+" MiB":value>=1024?number(value/1024)+" KiB":number(value)+" B";
const money=value=>value===null?"미산정":"$"+Number(value).toLocaleString("en-US",{minimumFractionDigits:2,maximumFractionDigits:4});
function node(tag,text,className){const el=document.createElement(tag);if(text!==undefined)el.textContent=text;if(className)el.className=className;return el;}
function replace(id,children){$(id).replaceChildren(...children);}
function cards(id,items){replace(id,items.map(([label,value,note])=>{const card=node("div",undefined,"card");card.append(node("div",label,"card-label"),node("div",value,"card-value"),node("small",note));return card;}));}
function stats(id,items){replace(id,items.map(([label,value])=>{const box=node("div",undefined,"stat");box.append(node("span",label),node("strong",value));return box;}));}
function table(id,headers,rows){const head=node("thead"),tr=node("tr"),body=node("tbody");headers.forEach(x=>tr.append(node("th",x)));head.append(tr);rows.forEach(row=>{const r=node("tr");row.forEach(x=>r.append(node("td",String(x))));body.append(r);});if(rows.length===0){const r=node("tr"),cell=node("td","보관된 요청이 없습니다.");cell.colSpan=headers.length;r.append(cell);body.append(r);}replace(id,[head,body]);}
function list(id,items){replace(id,items.map(x=>node("li",x)));}
function query(){const p=new URLSearchParams();for(const key of ["from","to"])if($(key).value)p.set(key,$(key).value);return p;}
function exports(){for(const format of ["json","csv"]){const p=query();p.set("format",format);$("export-"+format).href="/dev/metrics/export?"+p;}}
async function json(url,options){const response=await fetch(url,options);if(!response.ok){let code="HTTP "+response.status;try{code=(await response.json()).code||code;}catch{}throw new Error(code);}return response.json();}
function render(data){
    const o=data.overview,s=data.storage,p=data.process,d=o.diagnostics,l=data.estimatedFirestore;
    const count=key=>d[key]?.count||0,ms=key=>d[key]?.milliseconds||0;
    const incomplete=s.truncated||!s.persistenceAvailable||s.droppedRecords>0||s.invalidRecords>0;
    $("notice").className="notice"+(incomplete?" warning":"");
    $("notice").textContent=incomplete?`보관 표본에 누락이 있습니다 · 알려진 누락 ${number(s.droppedRecords)}건 / 손상 ${number(s.invalidRecords)}건 · 기록 저장 ${s.persistenceAvailable?"가능":"불가"}. 집계가 전체 사용량보다 작을 수 있습니다.`:"로컬 Mock 실측과 Firestore 가상 추정을 구분해 표시합니다. Firebase 운영 실측 없음.";
    $("updated").textContent="갱신 "+new Date(data.generatedUtc).toLocaleString("ko-KR");
    cards("summary",[["완료 API 요청",number(o.requests),`보관 표본 · 고유 사용자 ${number(o.activeUsers)}명`],["요청 지연 P95",number(o.p95Ms)+" ms",`P50 ${number(o.p50Ms)} · P99 ${number(o.p99Ms)} ms`],["실패 · 미완료",number(o.requests?o.failures/o.requests*100:0)+"%",`${number(o.failures)}건 · 중복 요청 ${number(o.duplicates)}건`],["HTTP 엔터티 전송",bytes(o.requestBytes+o.responseBytes),`요청 ${bytes(o.requestBytes)} / 응답 ${bytes(o.responseBytes)}`]]);
    const maximum=Math.max(1,...data.byDay.map(x=>x.requests));
    replace("daily",data.byDay.length?data.byDay.map(x=>{const row=node("div",undefined,"bar-row"),track=node("div",undefined,"bar-track"),fill=node("div",undefined,"bar-fill");fill.style.width=x.requests/maximum*100+"%";track.append(fill);row.append(node("span",x.key),track,node("span",number(x.requests)));return row;}):[node("div","게임 API를 호출하면 여기에 기록됩니다.","empty")]);
    stats("logical",[["문서 읽기",number(l.reads)],["문서 쓰기 / 삭제",number(l.writes)+" / "+number(l.deletes)],["인덱스 / 최소 쿼리 읽기",number(l.indexEntryReads)+" / "+number(l.queryMinimumReads)],["트랜잭션 시도",number(l.transactionAttempts)],["가정한 신규 저장량",bytes(l.storageBytes)],["미산정 요청",number(l.unsupported)]]);
    stats("physical",[["SQL 명령 실행",number(["sql-select","sql-insert","sql-update","sql-delete","sql-other"].reduce((n,k)=>n+count(k),0))],["읽고 변경한 행",number(count("sql-rows"))],["Commit / 벽시계 합계",number(count("transaction-commit"))+" / "+number(ms("transaction-commit"))+" ms"],["CAS 재시도 / busy 오류",number(count("cas-retry"))+" / "+number(count("sql-busy"))],["재현 벽시계 합계",number(ms("replay"))+" ms"],["제출 명령 / 로그 크기",number(o.commandCount)+" / "+bytes(o.logBytes)]]);
    stats("process",[["현재 / 최대 동시 요청",number(p.activeRequests)+" / "+number(p.peakRequests)],["프로세스 누적 CPU",number(p.processCpuMilliseconds)+" ms"],["프로세스 메모리 (Working set)",bytes(p.processMemoryBytes)],["권한 DB + WAL + SHM",bytes(s.authorityDatabaseBytes)],["계측 파일",bytes(s.metricsBytes)],["보관 / 최대 표본",number(s.retentionDays)+"일 / "+number(s.maxRecords)+"건"]]);
    const rows=groups=>groups.map(x=>[x.key,number(x.requests),number(x.failures),number(x.duplicates),number(x.averageMs),number(x.p95Ms),number(x.p99Ms),bytes(x.logBytes),number(x.diagnostics.replay?.milliseconds||0),number(["sql-select","sql-insert","sql-update","sql-delete","sql-other"].reduce((n,k)=>n+(x.diagnostics[k]?.count||0),0)),`${number(x.estimatedFirestore.reads)} / ${number(x.estimatedFirestore.writes)} / ${number(x.estimatedFirestore.deletes)}`,x.estimatedFirestore.estimatedDocumentOperationsUsd===null?"미산정":"$"+Number(x.estimatedFirestore.estimatedDocumentOperationsUsd).toFixed(6)]);
    const headers=["구분","요청","실패","중복","평균 ms","P95","P99","로그 bytes","재현 ms","SQL 실행","가상 R/W/D","문서 작업 USD"];
    table("endpoint-table",headers,rows(data.byEndpoint));table("run-table",headers,rows(data.byRun));
    table("daily-table",headers,rows(data.byDay));
    $("retention").textContent=`${s.retentionDays}일 · journal 최대 ${s.maxJournalFiles}개 × ${bytes(s.maxJournalBytes)} · 조회 최대 ${number(s.maxRecords)}건. 내보내기도 이 보관 한도를 따릅니다. 원본 ID와 본문은 저장하지 않습니다.`;
}
async function refresh(){const button=$("refresh");button.disabled=true;try{render(await json("/dev/metrics?"+query()));exports();}catch(error){$("notice").className="notice warning";$("notice").textContent="계측 조회 실패: "+error.message;}finally{button.disabled=false;}}
const advancedFields=[
    ["profileReadsPerUserPerDay","사용자당 일일 프로필 조회",2],["operationReadsPerRun","런당 작업 조회",.1],["runReadsPerRun","런당 런 상태 조회",.1],["growthPerUserPerDay","사용자당 일일 성장",.1],
    ["preAuthorityErrorFraction","오류 중 권한 진입 전 비율",.5],["duplicateSubmissionFraction","중복 중 새 ID 재제출 비율",.25],["profileBytes","프로필 bytes",2048],["runBytes","런 문서 bytes",2048],["receiptBytes","영수증 bytes",1024],["operationBytes","작업 문서 bytes",1024],["responseBytesPerRequest","응답 bytes",4096],["indexOverheadFactor","인덱스 추가 저장 비율",.5],["initialStoredGiB","초기 저장 GiB",0],["cpuCount","할당 vCPU",1]
];
for(const [name,label,value]of advancedFields){const el=node("label",label),input=node("input");input.name=name;input.type="number";input.value=value;input.min="0";input.step=/(Bytes|cpuCount)$/.test(name)?"1":"0.01";input.required=true;el.append(input);$("advanced").append(el);}
const lineNames={"firestore-reads":"Firestore 문서 읽기","firestore-writes":"Firestore 문서 쓰기","firestore-deletes":"Firestore 문서 삭제","firestore-storage":"Firestore 저장","firestore-same-region-egress":"Firestore 동일 리전 전송","cloud-run-cpu":"Cloud Run CPU","cloud-run-memory":"Cloud Run 메모리","cloud-run-requests":"Cloud Run 요청","cloud-run-egress":"Cloud Run → 한국 전송"};
async function calculate(event){event?.preventDefault();$("cost-error").textContent="";const button=$("scenario").querySelector("button[type=submit]");button.disabled=true;try{
    const scenario={region:"asia-northeast3",edition:"Standard",currency:"USD",applyFreeTier:$("scenario").elements.applyFreeTier.checked};
    for(const input of $("scenario").querySelectorAll("input[type=number]"))scenario[input.name]=Number(input.value);
    const result=await json("/dev/costs",{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify(scenario)});
    cards("cost-cards",[["최소 가정",money(result.minimum.totalUsd),"추정 범위 · 통계적 신뢰구간 아님"],["기준 가정",money(result.basis.totalUsd),`${scenario.days}일 · 무료 할당량 ${scenario.applyFreeTier?"적용 가정":"미차감"}`],["최악 가정",money(result.worst.totalUsd),"입력 오류·중복·재시도·크기를 확대"]]);
    table("cost-table",["비용 항목","모델 사용량","청구 가정량","단위","비용 USD"],result.basis.lineItems.map(x=>[lineNames[x.key]||x.key,number(x.quantity),number(x.billableQuantity),x.unit,money(x.costUsd)+(x.status==="estimated"?"":" · "+x.status)]));
    list("assumptions",result.assumptions);list("exclusions",result.excludedCosts);
}catch(error){$("cost-error").textContent="비용 계산 실패: "+error.message+". 입력 범위와 지원 요금표를 확인하세요.";replace("cost-cards",[]);table("cost-table",[],[]);}finally{button.disabled=false;}}
async function pricing(){try{const p=await json("/dev/pricing");$("price-version").textContent=`${p.version} · 확인 ${p.verifiedOn} · USD · 목적지 ${p.egressDestination}`;replace("sources",p.sources.map(url=>{const item=node("li"),a=node("a",url.replace("https://",""));const parsed=new URL(url);if(parsed.protocol==="https:"&&["cloud.google.com","firebase.google.com"].includes(parsed.hostname)){a.href=url;a.target="_blank";a.rel="noopener noreferrer";}item.append(a);return item;}));}catch{$("price-version").textContent="요금표를 읽을 수 없습니다. 비용을 0으로 간주하지 마세요.";}}
for(const button of document.querySelectorAll("nav button"))button.addEventListener("click",()=>{document.querySelectorAll("nav button").forEach(x=>x.classList.toggle("selected",x===button));document.querySelectorAll(".panel").forEach(x=>x.hidden=x.id!==button.dataset.panel);});
$("refresh").addEventListener("click",refresh);$("from").addEventListener("change",refresh);$("to").addEventListener("change",refresh);$("scenario").addEventListener("submit",calculate);
refresh();calculate();pricing();
