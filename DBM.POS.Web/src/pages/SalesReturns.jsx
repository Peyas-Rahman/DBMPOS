import { useEffect, useState } from "react";
import { returns, sales } from "../api";
import { Page, Panel, Table } from "../components/common";
import { money } from "../utils";
import { downloadSalesReturnReceipt } from "../utils/printInvoice";

const itemKey = item => `${item.productId}:${item.variantId || ""}`;

export default function SalesReturns() {
  const [query, setQuery] = useState("");
  const [invoices, setInvoices] = useState([]);
  const [selected, setSelected] = useState(null);
  const [history, setHistory] = useState([]);
  const [quantities, setQuantities] = useState({});
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [lastReturn, setLastReturn] = useState(null);

  async function searchInvoices() {
    try {
      const all = await sales.list();
      const value = query.trim().toLowerCase();
      setInvoices(value ? all.filter(invoice => `${invoice.invoiceNo || ""} ${invoice.customerName || ""} ${invoice.customerPhone || ""}`.toLowerCase().includes(value)) : all.slice(0, 30));
      setSelected(null);
    } catch (error) {
      setMessage(error.response?.data?.message || "Could not search invoices.");
    }
  }

  async function openInvoice(invoice) {
    try {
      const [detail, returnHistory] = await Promise.all([sales.get(invoice.id), returns.saleHistory(invoice.id)]);
      setSelected(detail);
      setHistory(returnHistory);
      const returned = new Map();
      returnHistory.forEach(row => returned.set(`${row.productId}:${row.variantId || ""}`, (returned.get(`${row.productId}:${row.variantId || ""}`) || 0) + Number(row.quantityIn || 0)));
      const next = {};
      detail.items.forEach(item => { next[itemKey(item)] = Math.max(0, Number(item.quantity) - (returned.get(itemKey(item)) || 0)); });
      setQuantities(next);
    } catch (error) {
      setMessage(error.response?.data?.message || "Could not load invoice.");
    }
  }

  useEffect(() => { searchInvoices().catch(() => {}); }, []);

  async function submitReturn(event) {
    event.preventDefault();
    if (!selected) return;
    const items = selected.items.map(item => ({ productId: item.productId, variantId: item.variantId, quantity: Number(quantities[`return:${itemKey(item)}`] || 0) })).filter(item => item.quantity > 0);
    if (!items.length) return setMessage("Enter a return quantity first.");
    setBusy(true);
    setMessage("");
    try {
      const result = await returns.sale(selected.id, { items });
      setLastReturn({ result, invoice: selected, items: selected.items.filter(item => items.some(returnItem => returnItem.productId === item.productId && returnItem.variantId === item.variantId)).map(item => ({ name: `${item.product?.productName || "Product"}${item.variant?.variantName ? ` / ${item.variant.variantName}` : ""}`, quantity: items.find(returnItem => returnItem.productId === item.productId && returnItem.variantId === item.variantId)?.quantity })) });
      setMessage(`Return completed: ${money(result.returnTotal)}. Customer credit: ${money(result.customerCredit)}. Current due: ${money(result.currentDue)}.`);
      await openInvoice(selected);
    } catch (error) {
      setMessage(error.response?.data?.message || "Sales return failed.");
    } finally {
      setBusy(false);
    }
  }

  return <Page title="Sales Return" eyebrow="SALES / RETURNS">
    {message && <div className="notice">{message}<button onClick={() => setMessage("")}>×</button></div>}
    {lastReturn && <button className="primary" onClick={() => downloadSalesReturnReceipt(lastReturn.invoice, lastReturn.result, lastReturn.items)}>Download Return Receipt</button>}
    <Panel title="Find Invoice">
      <div className="toolbar"><input value={query} onChange={event => setQuery(event.target.value)} onKeyDown={event => event.key === "Enter" && searchInvoices()} placeholder="Invoice number, customer name or phone" /><button className="primary" onClick={searchInvoices}>Search</button></div>
      <Table columns={[{ key: "invoiceNo", label: "Invoice" }, { key: "saleDate", label: "Date", render: row => new Date(row.saleDate).toLocaleString("en-BD") }, { key: "customerName", label: "Customer", render: row => row.customerName || "Walk-in Customer" }, { key: "grandTotal", label: "Total", render: row => money(row.grandTotal) }, { key: "open", label: "Action", render: row => <button className="smallbtn" onClick={() => openInvoice(row)}>Open</button> }]} rows={invoices} empty="No matching invoices" />
    </Panel>
    {selected && <div className="two">
      <Panel title={`Return items · ${selected.invoiceNo}`}>
        <form onSubmit={submitReturn}>
          <p><strong>Customer:</strong> {selected.customer?.customerName || "Walk-in Customer"}</p>
          <Table columns={[{ key: "product", label: "Product", render: row => `${row.product?.productName || "Product"}${row.variant?.variantName ? ` / ${row.variant.variantName}` : ""}` }, { key: "sold", label: "Sold", render: row => row.quantity }, { key: "remaining", label: "Remaining", render: row => quantities[itemKey(row)] }, { key: "return", label: "Return qty", render: row => <input type="number" min="0" max={quantities[itemKey(row)]} step="0.01" value={quantities[`return:${itemKey(row)}`] || ""} onChange={event => setQuantities(current => ({ ...current, [`return:${itemKey(row)}`]: event.target.value }))} /> }]} rows={selected.items.map(item => ({ ...item, quantity: item.quantity }))} />
          <button className="primary" disabled={busy}>{busy ? "Processing..." : "Complete return"}</button>
        </form>
      </Panel>
      <Panel title="Return History">
        <Table columns={[{ key: "createdAt", label: "Date", render: row => new Date(row.createdAt).toLocaleString("en-BD") }, { key: "productId", label: "Product" }, { key: "quantityIn", label: "Returned qty" }, { key: "referenceNo", label: "Invoice" }]} rows={history} empty="No returns for this invoice" />
      </Panel>
    </div>}
  </Page>;
}
