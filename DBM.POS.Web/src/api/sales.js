import api from "./client";

export async function getSales(params = {}) {
  const { data } = await api.get("/sales", { params });
  return data;
}

export async function createSale(payload) {
  const { data } = await api.post("/sales", payload);
  return data;
}
