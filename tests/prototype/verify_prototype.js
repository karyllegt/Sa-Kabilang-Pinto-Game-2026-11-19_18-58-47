// Usage: npm i puppeteer-core ; node verify_prototype.js /path/to/index.html /path/to/chrome
const puppeteer=require('puppeteer-core');
const HTML='file://'+require('path').resolve(process.argv[2]||'../../reference/prototype/index.html');
const CHROME=process.argv[3]||'/usr/bin/google-chrome';
const sl=ms=>new Promise(r=>setTimeout(r,ms));
const results=[];const rec=(name,ok,detail)=>{results.push({name,ok,detail:detail||''});console.log((ok?'PASS ':'FAIL ')+name+(detail?' - '+detail:''))};
(async()=>{
 const b=await puppeteer.launch({executablePath:CHROME,headless:'new',args:['--no-sandbox','--disable-gpu']});
 const p=await b.newPage();await p.setViewport({width:960,height:540});
 const errs=[];p.on('pageerror',e=>errs.push(e.message));p.on('console',m=>{if(m.type()==='error')errs.push('console: '+m.text())});
 // corrupt save must not break boot
 await p.evaluateOnNewDocument(()=>{try{localStorage.setItem('skp_save','{bad json')}catch(e){}});
 await p.goto(HTML);await sl(700);
 rec('boots with a corrupt save',await p.evaluate(()=>__skp.uiId==='title'));
 // generation + simulation for every level, difficulty
 const gen=await p.evaluate(()=>{const S=__skp,out=[];const ALL=['dj','veil','bolt','swift','heart','spread','veil2','glide'];
  for(const sk of [[],ALL]){S.save.skills=sk.slice();
   for(const df of ['story','standard','nightmare']){for(let i=0;i<9;i++){try{S.startLevel(i,df);S.beginPlay();for(let f=0;f<120;f++)S.stepGame();const G=S.G;
     const L=G.L;if(!(L.rooms>=4)||!L.lants.length||!L.doors.some(d=>d.kind==='exit'||L.cfg.boss))out.push('bad '+i+df);
     if(!isFinite(G.p.x)||!isFinite(G.p.y))out.push('NaN '+i+df)}catch(e){out.push('ERR '+i+df+' '+e.message)}}}}
  try{S.startLevel(-1,'endless',{loop:0});S.beginPlay();for(let f=0;f<120;f++)S.stepGame()}catch(e){out.push('ERR endless '+e.message)}
  return out});
 rec('all 9 levels x 3 difficulties x (no skills, all skills) build and simulate 120 frames; endless too',gen.length===0,gen.join('; '));
 // ui flow
 await p.evaluate(()=>{__skp.save.skills=[];__skp.save.unlocked=1;__skp.save.metLola=false;__skp.show('title')});
 await p.evaluate(()=>document.getElementById('bNew').click());await sl(300);
 rec('New Journey opens character select',await p.evaluate(()=>__skp.uiId==='chars'));
 await p.evaluate(()=>document.getElementById('bGirl').click());await sl(300);
 rec('character choice opens difficulty select',await p.evaluate(()=>__skp.uiId==='diffs'));
 await p.evaluate(()=>document.querySelector('[data-diff=standard]').click());await sl(1600);
 rec('difficulty opens level map with Lola first-meeting dialog',await p.evaluate(()=>__skp.uiId==='talk'));
 for(let i=0;i<10;i++){await p.keyboard.press('Enter');await sl(220);await p.keyboard.press('Enter');await sl(220)}
 // after dialogs the Enter key may start the level; ensure we reached the map or a level
 const st=await p.evaluate(()=>({ui:__skp.uiId,met:__skp.save.metLola}));
 rec('Lola dialog finishes and is remembered (metLola saved)',st.met===true,JSON.stringify(st));
 // enter level 1 explicitly
 await p.evaluate(()=>{__skp.openMap({})});await sl(700);
 await p.evaluate(()=>document.getElementById('mapGo').click());await sl(1500);
 rec('ENTER on map shows the before-stage dialog',await p.evaluate(()=>__skp.uiId==='story'));
 for(let i=0;i<4;i++){await p.keyboard.press('Enter');await sl(300)}
 rec('before-stage dialog starts gameplay',await p.evaluate(()=>__skp.uiId===null&&__skp.G&&__skp.G.state==='play'));
 // clue grants skill
 const sk=await p.evaluate(()=>{const G=__skp.G,c=G.L.items.find(i=>i.kind==='clue');G.p.x=c.x;G.p.y=c.y;__skp.stepGame();return {ui:__skp.uiId,sk:JSON.stringify(__skp.save.skills)}});
 rec('picking up the level 1 letter grants Double Jump and shows dialogs',sk.ui==='clueCard'&&sk.sk.includes('dj'),JSON.stringify(sk));
 for(let i=0;i<6;i++){await p.keyboard.press('Enter');await sl(250)}
 // double jump
 const dj=await p.evaluate(()=>{const G=__skp.G,I=__skp.I,pl=G.p;G.state='play';for(let i=0;i<60;i++)__skp.stepGame();
  I.jumpP=1;I.jump=1;__skp.stepGame();I.jumpP=0;for(let i=0;i<14;i++)__skp.stepGame();I.jumpP=1;__skp.stepGame();I.jumpP=0;const a=pl.air,vy=pl.vy;I.jump=0;
  I.jumpP=1;__skp.stepGame();I.jumpP=0;return {a,vy,after:pl.vy}});
 rec('double jump fires once in mid-air and not a third time',dj.a===1&&dj.vy<-6&&dj.after>-5,JSON.stringify(dj));
 // veil + bolt
 const vb=await p.evaluate(()=>{const G=__skp.G,I=__skp.I,pl=G.p;__skp.save.skills.push('veil','bolt');pl.veilCD=0;I.veilP=1;__skp.stepGame();I.veilP=0;const v=G.vit;pl.iv=0;__skp.hurt(1);const imm=G.vit===v;pl.boltCD=0;G.flame=50;I.boltP=1;__skp.stepGame();I.boltP=0;return {imm,bolts:G.bolts.length,flame:G.flame}});
 rec('veil blocks damage; bolt fires and costs 8 flame',vb.imm&&vb.bolts===1&&vb.flame===42,JSON.stringify(vb));
 // complete level -> clear box -> map
 await p.evaluate(()=>__skp.completeLevel());await sl(900);
 rec('completing a level shows the Level Clear box',await p.evaluate(()=>__skp.uiId==='complete'));
 const u=await p.evaluate(()=>__skp.save.unlocked);rec('next level unlocked',u>=2,'unlocked='+u);
 await p.evaluate(()=>document.getElementById('cMap').click());await sl(1600);
 rec('MAP button returns to the level map',await p.evaluate(()=>__skp.uiId==='map'||__skp.uiId==='talk'));
 // locked node
 await p.evaluate(()=>{__skp.save.unlocked=2;__skp.openMap({})});await sl(600);
 const lock=await p.evaluate(()=>{__skp.mapGoTo(8);return {sel:__skp.M.sel}});
 rec('map cannot select a locked node',lock.sel<=1,JSON.stringify(lock));
 // gameover/pause screens
 await p.evaluate(()=>{__skp.startLevel(0,'standard');__skp.beginPlay()});await sl(300);
 await p.evaluate(()=>document.getElementById('pauseBtn').click());await sl(300);
 rec('pause button opens pause screen and freezes game',await p.evaluate(()=>__skp.uiId==='pause'));
 await p.evaluate(()=>document.getElementById('pResume').click());await sl(300);
 rec('resume returns to play',await p.evaluate(()=>__skp.uiId===null));
 // ending
 await p.evaluate(()=>{__skp.startLevel(8,'standard');__skp.beginPlay();__skp.completeLevel()});await sl(900);
 const end=await p.evaluate(()=>({ui:__skp.uiId,cleared:__skp.save.cleared}));
 rec('finishing level 9 shows the ending and saves cleared',end.ui==='ending'&&end.cleared===true,JSON.stringify(end));
 // skill-obstacle solvability
 const sol=await p.evaluate(()=>{const S=__skp,I=S.I;const ALL=['dj','veil','bolt','swift','heart','spread','veil2','glide'];S.save.skills=ALL.slice();let gT=0,gO=0,tT=0,tO=0;
  for(let lv=1;lv<9;lv++){S.startLevel(lv,'standard');S.beginPlay();const G=S.G,L=G.L,p=G.p;G.state='play';
   const reset=(x,y)=>{for(const pl of L.plats){pl.dead=0;pl.t=0}p.x=x;p.y=y-p.h;p.vx=0;p.vy=0;p.ground=true;p.iv=0;p.veilT=0;p.veilCD=0;p.boltCD=0;p.air=0;G.vit=99;G.maxVit=99;G.flame=100;G.dim=S.ORD;G.state='play'};
   for(const h of L.haz.filter(h=>h.gaunt)){gT++;reset(h.x-8,h.y+7);const v0=G.vit;let ok=false;I.veilP=true;S.stepGame();I.veilP=false;for(let f=0;f<400;f++){I.right=true;S.stepGame();if(p.x>h.x+h.w+2){ok=true;break}}I.right=false;if(ok&&G.vit===v0)gO++}
   for(const h of L.haz.filter(h=>h.kind==='thorns')){tT++;const lp=L.plats.find(q=>q.m===1&&Math.abs(q.y-(h.y+h.h))<2&&q.x<=h.x&&q.x+q.w>=h.x+h.w);if(!lp)continue;const left=h.x-lp.x<(lp.x+lp.w)-(h.x+h.w);reset(left?lp.x+lp.w-14:lp.x+2,h.y+h.h);p.face=h.x>p.x?1:-1;I.boltP=true;S.stepGame();I.boltP=false;let bu=false;for(let f=0;f<60;f++){S.stepGame();if(h.burn>0){bu=true;break}}if(bu)tO++}}
  return {gT,gO,tT,tO}});
 rec('every veil spike field is crossable under the veil; every thorn wall burns with the bolt',sol.gO===sol.gT&&sol.tO===sol.tT,JSON.stringify(sol));
 rec('no JavaScript errors during the whole run',errs.length===0,errs.slice(0,3).join(' | '));
 await b.close();
 const pass=results.filter(r=>r.ok).length;
 require('fs').writeFileSync('prototype_verification_report.json',JSON.stringify({passed:pass,total:results.length,results},null,2));
 console.log('\n'+pass+'/'+results.length+' checks passed');
 process.exit(pass===results.length?0:1);
})();
