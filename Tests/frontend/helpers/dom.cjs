class Event { constructor(type,options={}){this.type=type;Object.assign(this,options);}preventDefault(){this.prevented=true;}stopPropagation(){this.stopped=true;} }
class Node {
 constructor(tag,doc){this.tagName=tag.toUpperCase();this.doc=doc;this.children=[];this.attrs={};this.dataset={};this.events={};this.style={};this._value='';this.files=[];this.checked=false;this.classList={toggle(){},add(){},remove(){}};}
 get id(){return this.attrs.id;}set id(v){this.attrs.id=v;}
 get value(){return this.tagName==='SELECT'&&!this._value?(this.options[0]?.value||''):this._value;}set value(v){this._value=String(v??'');}
 get options(){return this.all().filter(n=>n.tagName==='OPTION');}
 get selectedOptions(){return this.options.filter(n=>n.value===this.value);}
 get labels(){return this.doc.root.all().filter(n=>n.tagName==='LABEL'&&(n.attrs.for===this.id||n.contains(this)));}
 set textContent(v){this._text=String(v);this.children=[];}get textContent(){return(this._text||'')+this.children.map(c=>c.textContent).join('');}
 get validity(){return{valid:!this.customValidity};}setCustomValidity(v){this.customValidity=v;}reportValidity(){return !this.customValidity;}
 append(...nodes){for(const node of nodes){node.parent=this;this.children.push(node);}}
 before(node){node.parent=this.parent;this.parent.children.splice(this.parent.children.indexOf(this),0,node);}
 replaceChildren(){this.children=[];}
 contains(node){return this===node||this.children.some(c=>c.contains(node));}
 all(){return this.children.flatMap(c=>[c,...c.all()]);}
 setAttribute(k,v){this.attrs[k]=String(v);if(k.startsWith('data-'))this.dataset[k.slice(5).replace(/-([a-z])/g,(_,c)=>c.toUpperCase())]=String(v);}
 removeAttribute(k){delete this.attrs[k];}
 matches(s){if(/^[a-z]+$/i.test(s))return this.tagName===s.toUpperCase();if(s.startsWith('.'))return String(this.attrs.class||this.className||'').split(/\s+/).includes(s.slice(1));if(s.startsWith('#'))return this.id===s.slice(1);if(s.endsWith(':checked')&&!this.checked)return false;const pairs=[...s.matchAll(/\[([\w-]+)(?:="([^"]*)")?\]/g)];return pairs.length>0&&pairs.every(([,k,v])=>k in this.attrs&&(v===undefined||this.attrs[k]===v));}
 querySelectorAll(s){return this.all().filter(n=>n.matches(s));}querySelector(s){return this.querySelectorAll(s)[0]||null;}
 addEventListener(name,fn){(this.events[name]||=[]).push(fn);}
 dispatchEvent(event){event.target||=this;for(const fn of this.events[event.type]||[])fn(event);if(event.bubbles&&!event.stopped)this.parent?.dispatchEvent(event);}
 async emit(name,props={}){const event=new Event(name,props);event.target=this;event.currentTarget=this;await Promise.all((this.events[name]||[]).map(fn=>fn(event)));}
 insertAdjacentHTML(where,html){if(where!=='beforeend')throw Error('Position not supported by test DOM');const fragment=new Node('div',this.doc);fragment.innerHTML=html;this.html=(this.html||'')+html;this.append(...fragment.children);}
 focus(){if(this.doc.activeElement!==this){this.doc.activeElement=this;this.dispatchEvent(new Event('focus'));}}
 scrollIntoView(){}
 set innerHTML(html){this.html=html;this.children=[];this._text='';const stack=[this];const voids=new Set(['INPUT','IMG','BR','HR','META','LINK']);for(const token of html.match(/<[^>]+>|[^<]+/g)||[]){if(token.startsWith('</')){if(stack.length>1)stack.pop();continue;}if(token.startsWith('<')){const match=token.match(/^<([\w-]+)([^>]*)>/);if(!match)continue;const node=new Node(match[1],this.doc);for(const[,k,v]of match[2].matchAll(/([\w-]+)(?:="([^"]*)")?/g)){node.setAttribute(k,v??'');if(k==='value')node.value=v;if(['checked','disabled','hidden','required','open'].includes(k))node[k]=true;}stack.at(-1).append(node);if(!voids.has(node.tagName))stack.push(node);}else stack.at(-1)._text=(stack.at(-1)._text||'')+token;}}
 get innerHTML(){return this.html||'';}
}
function document(){const doc={activeElement:null,addEventListener(){},createElement:tag=>new Node(tag,doc),getElementById:id=>doc.root.all().find(n=>n.id===id)||null,querySelector:s=>doc.root.querySelector(s),querySelectorAll:s=>doc.root.querySelectorAll(s)};doc.root=new Node('body',doc);doc.body=doc.root;return doc;}
module.exports={Node,Event,document};
