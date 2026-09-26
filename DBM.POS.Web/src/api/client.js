import axios from "axios";
const api=axios.create({baseURL:import.meta.env.VITE_API_URL||"http://localhost:5000/api",headers:{"Content-Type":"application/json"},timeout:20000});
api.interceptors.request.use(c=>{const t=localStorage.getItem("token");if(t)c.headers.Authorization=`Bearer ${t}`;return c});
api.interceptors.response.use(r=>r,e=>{if(e.response?.status===401){localStorage.removeItem("token");localStorage.removeItem("user");window.location="/login"}return Promise.reject(e)});
export default api;