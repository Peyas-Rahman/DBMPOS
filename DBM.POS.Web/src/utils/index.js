export const money=v=>`৳${Number(v||0).toLocaleString('en-BD',{minimumFractionDigits:2,maximumFractionDigits:2})}`;
export const today=()=>new Date().toISOString().slice(0,10);
