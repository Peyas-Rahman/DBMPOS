import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { sales } from "../api";
import { Page, Panel, Table } from "../components/common";
import { money } from "../utils";
import { printInvoice } from "../utils/printInvoice";

export default function Sales() {
  const [invoices, setInvoices] = useState([]);
  const [selected, setSelected] = useState(null);
  const [message, setMessage] = useState("");
  const [searchParams] = useSearchParams();

  useEffect(() => {
    sales.list().then(items => {
      setInvoices(items);
      const invoiceId = searchParams.get("invoiceId");
      if (invoiceId) return sales.get(invoiceId).then(setSelected);
      return null;
    }).catch(error => setMessage(error.response?.data?.message || "Could not load invoices."));
  }, [searchParams]);

  async function openInvoice(id) {
    try {
      setSelected(await sales.get(id));
    } catch (error) {
      setMessage(error.response?.data?.message || "Could not load invoice details.");
    }
  }

  async function printSavedInvoice(id) {
    try {
      const invoice = await sales.get(id);
      printInvoice(invoice, { customer: invoice.customer, soldBy: invoice.user?.fullName });
    } catch (error) {
      setMessage(error.response?.data?.message || "Could not load invoice for printing.");
    }
  }

  return <Page title="Manage Invoices" eyebrow="SALES">
    {message && <div className="notice">{message}<button onClick={() => setMessage("")}>×</button></div>}
    <Panel title="Sales Invoices">
      <Table columns={[
        { key: "invoiceNo", label: "Invoice" },
        { key: "saleDate", label: "Date", render: row => new Date(row.saleDate).toLocaleString("en-BD") },
        { key: "customerName", label: "Customer", render: row => row.customerName || "Walk-in Customer" },
        { key: "customerPhone", label: "Phone", render: row => row.customerPhone || "-" },
        { key: "soldBy", label: "Sold By", render: row => row.soldBy || "-" },
        { key: "grandTotal", label: "Total", render: row => money(row.grandTotal) },
        { key: "paidAmount", label: "Paid", render: row => money(row.paidAmount) },
        { key: "dueAmount", label: "Due", render: row => money(row.dueAmount) },
        { key: "paymentStatus", label: "Status" },
        { key: "actions", label: "Actions", render: row => <><button className="smallbtn" onClick={() => openInvoice(row.id)}>View</button> <button className="smallbtn" onClick={() => printSavedInvoice(row.id)}>Print / PDF</button></> }
      ]} rows={invoices} />
    </Panel>
    {selected && <div className="modal"><div className="modalbox"><button className="close" onClick={() => setSelected(null)}>×</button><h2>{selected.invoiceNo}</h2><p><strong>Customer:</strong> {selected.customer?.customerName || "Walk-in Customer"}</p>{selected.customer?.phone && <p><strong>Phone:</strong> {selected.customer.phone}</p>}{selected.customer?.address && <p><strong>Address:</strong> {selected.customer.address}</p>}<p><strong>Date:</strong> {new Date(selected.saleDate).toLocaleString("en-BD")}</p><div className="tablewrap"><table><thead><tr><th>Product</th><th>Qty</th><th>Price</th><th>Discount</th><th>Total</th></tr></thead><tbody>{selected.items?.map(item => <tr key={item.id}><td>{item.product?.productName || "Product"}{item.variant?.variantName ? ` / ${item.variant.variantName}` : ""}</td><td>{item.quantity}</td><td>{money(item.unitPrice)}</td><td>{money(item.discount)}</td><td>{money(item.lineTotal)}</td></tr>)}</tbody></table></div><p>Subtotal: {money(selected.subTotal)} · Discount: {money(selected.discount)} · Tax: {money(selected.tax)}</p><p><strong>Grand Total: {money(selected.grandTotal)}</strong></p><p>Paid: {money(selected.paidAmount)} · Due: {money(selected.dueAmount)} · {selected.paymentStatus}</p><button className="primary" onClick={() => printInvoice(selected, { customer: selected.customer })}>Print / Save PDF</button></div></div>}
  </Page>;
}
