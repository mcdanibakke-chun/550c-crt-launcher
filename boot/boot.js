'use strict';
// Independently implemented visual story. No client operations or diagnostics.
(() => {
 const c=window.BOOT_CONFIG,t=c.text,$=id=>document.getElementById(id),post=data=>window.chrome?.webview?.postMessage(data),scale=16000/c.durationMs;
 for(const [key,value] of Object.entries(c.colors))document.documentElement.style.setProperty('--'+(key==='background'?'bg':key),value);
 document.body.style.fontFamily=c.fontFamily;
 const fields={introCaption:'bootCaption',brand:'brand',product:'product',mode:'headerMode',telemetryTitle:'telemetryTitle',terminalTitle:'terminalTitle',nodesTitle:'nodesTitle',nodeCaption:'nodeCaption',signalNote:'signalNote',fictional:'fictional',skipHint:'skipHint',finalProduct:'product',finalText:'final',initialized:'initialized'};
 for(const [id,key] of Object.entries(fields))$(id).textContent=t[key];
 const ribbons=[...$('ribbons').children],clips=[...$('mark').querySelectorAll('clipPath rect')],clipBoxes=ribbons.map(p=>p.getBBox());
 ribbons.forEach((p,i)=>{const copy=p.cloneNode(true);copy.setAttribute('clip-path','url(#reveal'+i+')');$('litRibbons').append(copy);});
 if(t.symbol!=='550C'){ribbons.forEach(p=>p.style.display='none');$('litRibbons').replaceChildren();const s=document.createElementNS('http://www.w3.org/2000/svg','text');s.setAttribute('x',400);s.setAttribute('y',165);s.setAttribute('font-size',140);s.setAttribute('text-anchor','middle');s.setAttribute('fill',c.colors.logoWhite);s.textContent=t.symbol;$('litRibbons').append(s);}
 for(const [key,value] of t.telemetryRows){const row=document.createElement('div');row.className='metric'+(value===''?' group':'');const a=document.createElement('span'),b=document.createElement('b');a.textContent=key;b.textContent=value;row.append(a,b);$('metrics').append(row);}
 // The gray CRT reference leaves telemetry mostly open; no ornamental radar/bars.
 const cells=[];
 for(let i=0;i<47;i++){const el=document.createElement('div');el.className='cell';el.innerHTML='<svg viewBox="0 0 36 28"><path d="M12 10 L24 18 M24 10 L12 18 M11 9 H25 V19 H11 Z M6 5 L11 10 M30 5 L25 10 M6 23 L11 18 M30 23 L25 18"/><ellipse cx="6" cy="5" rx="5" ry="3"/><ellipse cx="30" cy="5" rx="5" ry="3"/><ellipse cx="6" cy="23" rx="5" ry="3"/><ellipse cx="30" cy="23" rx="5" ry="3"/></svg>';const label=document.createElement('div'),state=document.createElement('small'),bars=document.createElement('div');bars.className='nodeBars';for(let j=0;j<5;j++){const bar=document.createElement('i');bar.style.height=(j+2)+'px';bars.append(bar);}label.textContent=t.nodePrefix+String(i+1).padStart(2,'0');state.textContent=t.nodeIdle;el.append(bars,label,state);$('nodes').append(el);cells.push({el,state});}
 const logs=t.logs.map(([at,text,color])=>{const el=document.createElement('div');el.className='log '+(color||'');const stamp=document.createElement('time'),content=document.createElement('span');const secs=Math.floor(at/1000);stamp.textContent='00:00:'+String(secs).padStart(2,'0');el.append(stamp,content);$('terminal').append(el);return{at,text,el,content};});
 const popups=t.popups.map((p,index)=>{
  const el=document.createElement('section');el.className='popup '+p.color;el.style.left=p.left;el.style.top=p.top;el.style.zIndex=p.layer;if(p.width)el.style.width=p.width;
  const state={time:0,closedAt:null,flashUntil:0,layoutPosted:false,minimized:false,maximized:false};
  const header=document.createElement('div');header.className='popupHeader';header.textContent=p.title;
  const controls=document.createElement('span');controls.className='windowControls';
  for(const role of ['minimize','maximize','close']){
   const button=document.createElement('button');button.type='button';button.dataset.role=role;button.textContent=t.windowControls[role+'Symbol'];button.title=t.windowControls[role];button.setAttribute('aria-label',button.title);
   button.addEventListener('pointerdown',e=>e.stopPropagation());
   button.addEventListener('click',e=>{e.stopPropagation();state.flashUntil=state.time+160;let action=role;
    if(role==='close')state.closedAt=state.time;
    if(role==='minimize'){state.minimized=!state.minimized;if(state.minimized)state.maximized=false;action=state.minimized?'minimize':'restore';}
    if(role==='maximize'){state.maximized=!state.maximized;state.minimized=false;action=state.maximized?'maximize':'restore';}
    el.classList.toggle('minimized',state.minimized);el.classList.toggle('maximized',state.maximized);state.layoutPosted=false;
    right.textContent=t.windowControls[action+'Status'];post({event:'window-control',popup:index,action});
   });controls.append(button);
  }header.append(controls);
  const body=document.createElement('div');body.className='popupBody';const caption=document.createElement('strong');caption.textContent=p.headline;body.append(caption);
  for(const line of p.lines){const node=document.createElement('p');node.textContent=line;body.append(node);}
  let counter,progress,mini;
  if(p.displayCount!==undefined){const statistic=document.createElement('div');statistic.className='popupCounter';statistic.append(String(p.displayCount));const label=document.createElement('small');label.textContent=p.countLabel;statistic.append(label);body.append(statistic);}
  if(p.rows){const rows=document.createElement('div');rows.className='popupRows';p.rows.forEach(pair=>pair.forEach(text=>{const node=document.createElement('span');node.textContent=text;rows.append(node);}));body.append(rows);}
  if(p.progress){counter=document.createElement('div');counter.className='popupCounter';body.append(counter);const track=document.createElement('div');track.className='popupProgress';progress=document.createElement('i');track.append(progress);body.append(track);}
  if(p.miniLog){mini=document.createElement('div');mini.className='miniLog';for(const text of p.miniLog){const line=document.createElement('div');line.textContent=text;mini.append(line);}body.append(mini);}
  if(p.wave){const wave=document.createElement('div');wave.className='popupWave';for(let i=0;i<48;i++){const bar=document.createElement('i');bar.style.height=(15+i*41%85)+'%';wave.append(bar);}body.append(wave);}
  if(p.notice){const notice=document.createElement('p');notice.className='notice';notice.textContent=p.notice;body.append(notice);}
  const status=document.createElement('div');status.className='popupStatus';const left=document.createElement('span'),right=document.createElement('span');left.textContent=t.popupDisclaimer;right.textContent=p.status;status.append(left,right);
  if(p.buttons){const buttons=document.createElement('div');buttons.className='popupButtons';for(const item of p.buttons){const button=document.createElement('button');button.type='button';button.textContent=item.label;button.addEventListener('pointerdown',e=>e.stopPropagation());button.addEventListener('click',e=>{e.stopPropagation();state.flashUntil=state.time+230;right.textContent=item.response;if(item.action==='close')state.closedAt=state.time;else if(item.action==='details'&&mini)mini.hidden=!mini.hidden;else{button.textContent=item.response;button.disabled=true;}post({event:'button',popup:index,action:item.action,label:item.label});});buttons.append(button);}body.append(buttons);}
  el.append(header,body,status);$('popups').append(el);return{p,el,counter,progress,state,index};
 });
 let start=0,frame=0,ended=false,ready=false,frames=0,worstGap=0,previous=0,lastCode=-1,lastNodes=-1,lastPhase=-1;
 const phaseTimes=[0,4050,5000,6250,7650,8200,8800,11200,11900,12400,13200,14000];
 const clamp=(n,a=0,b=1)=>Math.min(b,Math.max(a,n)),ease=n=>{const u=clamp(n);return u*u*(3-2*u);};
 function render(ms){
  const n=ms*scale;
  clips.forEach((clip,i)=>{const box=clipBoxes[i];clip.setAttribute('x',box.x-10);clip.setAttribute('width',(box.width+20)*ease((n-250-i*300)/650));});
  $('introCaption').style.opacity=ease((n-2400)/450);$('intro').style.opacity=ease((4050-n)/600);$('console').style.opacity=ease((n-3680)/470);
  let phase=0;phaseTimes.forEach((at,i)=>{if(n>=at)phase=i;});if(lastPhase!==phase){lastPhase=phase;$('phase').textContent=t.phaseNames[phase];}
  for(const log of logs){const chars=Math.max(0,Math.floor((n-log.at)/(c.typingMsPerCharacter||7)));const value=log.text.slice(0,chars);if(log.content.textContent!==value)log.content.textContent=value;log.el.style.display=n>=log.at?'block':'none';}if(n>4050)$('terminal').scrollTop=$('terminal').scrollHeight;
  const step=Math.floor(Math.max(0,n-4050)/80);if(step!==lastCode){lastCode=step;$('code').replaceChildren();for(let i=Math.max(0,step-28);i<=step;i++){const node=document.createElement('div'),address=document.createElement('span');address.className='address';address.textContent=(0x3000+i*8).toString(16).padStart(8,'0');node.append(address,String(i).padStart(4,'0')+'  '+t.signalLines[i%t.signalLines.length]);$('code').append(node);}$('code').scrollTop=$('code').scrollHeight;}
  const nodes=Math.floor(47*clamp((n-8800)/2400));if(nodes!==lastNodes){lastNodes=nodes;cells.forEach(({el,state},i)=>{el.className='cell '+(i<nodes?'ready':i===nodes&&n>=8800?'active':'');state.textContent=i<nodes?t.nodeDone:i===nodes&&n>=8800?t.nodeActive:t.nodeIdle;});if(nodes>0){const active=cells[Math.min(nodes,46)].el;$('nodes').scrollTop=Math.max(0,active.offsetTop-$('nodes').offsetTop-$('nodes').clientHeight+active.clientHeight+8);}}
  const percent=nodes/47*100;$('nodeCount').textContent=String(nodes).padStart(2,'0')+' / 47 '+t.progressUnit;$('percentage').textContent=Math.round(percent)+'%';$('meter').style.width=percent+'%';
  for(const {p,el,counter,progress,state,index} of popups){state.time=n;const manualFade=state.closedAt===null?1:ease((state.closedAt+180-n)/180);const show=n>=p.at&&n<p.until+120&&manualFade>0;const presence=ease((n-p.at)/170)*ease((p.until+120-n)/220)*manualFade;el.classList.toggle('confirmed',show&&n<state.flashUntil);el.style.opacity=presence;el.style.transform='translateY('+(7*(1-presence))+'px) scale('+(.987+.013*presence)+')';el.classList.toggle('visible',show);el.classList.toggle('noiseHit',show&&n%1370<65);if(show){const w=el.offsetWidth,h=el.offsetHeight;el.style.left=Math.max(8,Math.min(parseFloat(p.left)/100*innerWidth,innerWidth-w-12))+'px';el.style.top=Math.max(8,Math.min(parseFloat(p.top)/100*innerHeight,innerHeight-h-34))+'px';}if(state.maximized){el.style.left='12px';el.style.top='12px';}if(show&&!state.layoutPosted&&presence>=.999){state.layoutPosted=true;post({event:'button-layout',popup:index,buttons:[...el.querySelectorAll('.popupButtons button')].map(button=>{const r=button.getBoundingClientRect();return{left:r.left,top:r.top,width:r.width,height:r.height};}),controls:[...el.querySelectorAll('.windowControls button')].map(button=>{const r=button.getBoundingClientRect();return{role:button.dataset.role,left:r.left,top:r.top,width:r.width,height:r.height};}),minimized:state.minimized,maximized:state.maximized,viewport:{width:innerWidth,height:innerHeight}});}if(counter&&show){const count=p.progress==='nodes'?nodes:Math.ceil(Math.max(0,14000-n)/270);if(counter.dataset.count!==String(count)){counter.dataset.count=String(count);counter.replaceChildren();counter.append(String(count).padStart(2,'0'));const unit=document.createElement('small');unit.textContent=p.progress==='nodes'?'/ 47 '+t.progressUnit:t.countdownUnit;counter.append(unit);}progress.style.width=(p.progress==='nodes'?percent:100*clamp((n-p.at)/(p.until-p.at)))+'%';}}
  const final=ease((n-14000)/220);$('final').style.opacity=final;$('final').style.clipPath='inset(0 '+(50*(1-ease((n-14000)/650)))+'%)';
  const disturbed=n>6500&&n<13800&&((n%2230<45)||(n>7100&&n<7420&&n%190<25));$('console').style.transform=disturbed?'translateX('+(n%2<1?-1:2)+'px) skewX(.08deg)':'none';$('console').style.filter=disturbed?'brightness(1.09)':'none';
 }
 let clockOffset=0;
 function tick(now){if(ended)return;if(!start)start=now-clockOffset;const elapsed=now-start;frames++;if(previous)worstGap=Math.max(worstGap,now-previous);previous=now;render(elapsed);if(elapsed>=c.durationMs){ended=true;post({event:'complete',elapsedMs:elapsed,frames,worstGapMs:worstGap});return;}frame=requestAnimationFrame(tick);}
 function skip(source){if(ended)return;ended=true;cancelAnimationFrame(frame);post({event:'skip',source});}
 document.addEventListener('keydown',e=>{if(e.key==='Escape'){e.preventDefault();skip('esc');}});document.addEventListener('pointerdown',e=>{if(!e.target.closest('button'))skip('click');});window.addEventListener('error',e=>post({event:'error',message:e.message}));
 window.proof={start(offset=0){if(ready)return;ready=true;clockOffset=offset;render(offset);frame=requestAnimationFrame(tick);},skip,waiting(minimal=false){ended=true;cancelAnimationFrame(frame);render(c.durationMs);if(minimal){$('console').style.opacity='0';$('popups').style.display='none';}$('final').style.opacity='1';$('final').style.clipPath='none';$('finalText').textContent=minimal?t.launcher.waitingMinimal:t.launcher.waiting;$('initialized').textContent=t.launcher.waitingDetail;},renderAt(ms){ended=true;cancelAnimationFrame(frame);render(ms);},stats(){return{frames,worstGapMs:worstGap};},testInteractions(kind){
  // This tests this proof's own DOM only; it sends no input to any desktop app.
  cancelAnimationFrame(frame);render(5700);ended=false;
  const checks=[];const check=(name,value)=>checks.push({name,pass:!!value});
  if(kind==='buttons'){
   const p=popups[0],el=p.el,control=role=>el.querySelector('[data-role="'+role+'"]');
   const first=el.querySelector('.popupButtons button');first.click();check('confirm response',first.disabled);check('buttons do not skip',!ended);
   control('minimize').click();check('minimize',p.state.minimized&&el.classList.contains('minimized'));control('minimize').click();check('restore minimized',!p.state.minimized);
   control('maximize').click();render(5700);check('maximize',p.state.maximized&&el.offsetWidth>=innerWidth-26);control('maximize').click();render(5700);check('restore maximized',!p.state.maximized);
   control('close').click();render(5950);check('close own popup',!el.classList.contains('visible'));check('window controls do not skip',!ended);
   render(7400);const details=popups[2];details.el.querySelector('.popupButtons button').click();check('details response',details.el.querySelector('.miniLog').hidden);
  }else if(kind==='esc'){
   document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true,cancelable:true}));check('Escape handler',ended);
  }else if(kind==='click'){
   $('screen').dispatchEvent(new PointerEvent('pointerdown',{bubbles:true}));check('background click handler',ended);
  }else throw new Error('Unknown local test');
  const result={kind,checks,pass:checks.length>0&&checks.every(x=>x.pass)};post({event:'self-test',...result});return result;
 }};
 render(0);post({event:'ready'});
})();







