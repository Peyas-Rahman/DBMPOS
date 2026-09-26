import api from "./client";

export async function getProducts(search = "") {
  const { data } = await api.get("/products", {
    params: { search, activeOnly: true }
  });
  return Array.isArray(data) ? data : [];
}

export async function getProductByBarcode(barcode) {
  const { data } = await api.get(`/products/barcode/${encodeURIComponent(barcode)}`);
  return data;
}
