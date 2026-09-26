import { useEffect, useState } from "react";
import { customers, org, sales, suppliers } from "../api";
import { Page, Panel, Table, Field, Select } from "../components/common";
import { money } from "../utils";
import { downloadPaymentReceipt, printInvoice } from "../utils/printInvoice";

const emptyForm = { Code: "", Name: "", Phone: "", Email: "", Address: "", CreditLimit: 0, OpeningDue: 0, DiscountPercent: 0 };
const emptyPayment = { amount: "", paymentMethod: "Cash", referenceNo: "", notes: "", branchId: "" };

export default function Parties({ type = "customer" }) {
  const isCustomer = type === "customer";
  const api = isCustomer ? customers : suppliers;
  const [data, setData] = useState([]);
  const [branches, setBranches] = useState([]);
  const [form, setForm] = useState(emptyForm);
  const [message, setMessage] = useState("");
  const [selected, setSelected] = useState(null);
  const [balance, setBalance] = useState(null);
  const [ledger, setLedger] = useState([]);
  const [customerSales, setCustomerSales] = useState([]);
  const [payment, setPayment] = useState(emptyPayment);
  const [savingPayment, setSavingPayment] = useState(false);
  const [search, setSearch] = useState("");
  const [editing, setEditing] = useState(null);

  async function loadParties(value = search) {
    setData(await api.list(value));
  }

  useEffect(() => {
    loadParties(search).catch(error => setMessage(error.response?.data?.message || "Could not load records."));
    if (isCustomer) org.branches().then(setBranches).catch(() => {});
  }, [type, search]);

  async function saveParty(event) {
    event.preventDefault();
    setMessage("");
    try {
      if (editing) await api.update(editing.id, form);
      else await api.create(form);
      setForm(emptyForm);
      setEditing(null);
      await loadParties();
      setMessage(editing ? "Updated successfully." : "Saved successfully.");
    } catch (error) {
      setMessage(error.response?.data?.message || "Save failed.");
    }
  }

  async function deleteParty(party) {
    if (isCustomer || !window.confirm(`Delete supplier ${party.supplierName}?`)) return;
    try {
      await suppliers.del(party.id);
      await loadParties();
      setMessage("Supplier deleted successfully.");
    } catch (error) {
      setMessage(error.response?.data?.message || "Supplier deletion failed.");
    }
  }

  async function selectCustomer(customer) {
    setSelected(customer);
    setMessage("");
    setPayment(emptyPayment);
    try {
      const [currentBalance, entries, invoices] = await Promise.all([customers.due(customer.id), customers.ledger(customer.id), customers.sales(customer.id)]);
      setBalance(currentBalance);
      setLedger(entries);
      setCustomerSales(invoices);
    } catch (error) {
      setBalance(null);
      setCustomerSales([]);
      setMessage(error.response?.data?.message || "Could not load customer due.");
    }
  }

  async function receivePayment(event) {
    event.preventDefault();
    if (!selected || !balance) return;
    setSavingPayment(true);
    setMessage("");
    try {
      const result = await customers.pay(selected.id, {
        amount: Number(payment.amount),
        paymentMethod: payment.paymentMethod,
        referenceNo: payment.referenceNo || null,
        notes: payment.notes || null,
        branchId: payment.branchId || null
      });
      const [currentBalance, entries] = await Promise.all([customers.due(selected.id), customers.ledger(selected.id)]);
      setBalance(currentBalance);
      setLedger(entries);
      setPayment(emptyPayment);
      setMessage(`Payment recorded: ${money(result.amount)}. Remaining due: ${money(result.currentDue)}.`);
    } catch (error) {
      setMessage(error.response?.data?.message || "Payment could not be recorded.");
    } finally {
      setSavingPayment(false);
    }
  }

  return <Page title={isCustomer ? "Customers" : "Suppliers"} eyebrow={isCustomer ? "CRM / CLIENTS" : "STOCK / SUPPLIERS"}>
    {message && <div className="notice">{message}<button onClick={() => setMessage("")}>×</button></div>}
    <div className="two">
      <Panel title={editing ? `Edit ${isCustomer ? "Customer" : "Supplier"}` : isCustomer ? "New Customer" : "New Supplier"}>
        <form onSubmit={saveParty}>
          <Field label="Code" value={form.Code} onChange={event => setForm({ ...form, Code: event.target.value })} required />
          <Field label="Name" value={form.Name} onChange={event => setForm({ ...form, Name: event.target.value })} required />
          <Field label="Phone" value={form.Phone} onChange={event => setForm({ ...form, Phone: event.target.value })} />
          <Field label="Email" value={form.Email} onChange={event => setForm({ ...form, Email: event.target.value })} />
          <Field label="Address" value={form.Address} onChange={event => setForm({ ...form, Address: event.target.value })} />
          {isCustomer && <Field label="Opening Due" type="number" min="0" step="0.01" value={form.OpeningDue} onChange={event => setForm({ ...form, OpeningDue: Number(event.target.value) })} />}
          {isCustomer && <Field label="Credit Limit" type="number" min="0" step="0.01" value={form.CreditLimit} onChange={event => setForm({ ...form, CreditLimit: Number(event.target.value) })} />}
          {isCustomer && <Field label="Membership Discount %" type="number" min="0" max="100" step="0.01" value={form.DiscountPercent} onChange={event => setForm({ ...form, DiscountPercent: Number(event.target.value) })} />}
          <button className="primary">{editing ? "Update" : "Save"}</button>
          {editing && <button type="button" onClick={() => { setEditing(null); setForm(emptyForm); }}>Cancel</button>}
        </form>
      </Panel>
      <Panel title={isCustomer ? "Customer List" : "Supplier List"}>
        <div className="toolbar"><input placeholder={`Search ${isCustomer ? "customers" : "suppliers"}...`} value={search} onChange={event => setSearch(event.target.value)} /></div>
        <Table columns={[
          { key: isCustomer ? "customerCode" : "supplierCode", label: "Code" },
          { key: isCustomer ? "customerName" : "supplierName", label: "Name" },
          { key: "phone", label: "Phone" },
          { key: "openingDue", label: "Opening Due", render: row => money(row.openingDue) },
          ...(isCustomer ? [{ key: "currentDue", label: "Current Due", render: row => money(row.currentDue) }] : []),
          ...(isCustomer ? [{ key: "dueAction", label: "Due", render: row => <button className="smallbtn" onClick={() => selectCustomer(row)}>View due / Receive</button> }] : [{ key: "actions", label: "Actions", render: row => <><button className="smallbtn" onClick={() => { setEditing(row); setForm({ Code: row.supplierCode, Name: row.supplierName, Phone: row.phone || "", Email: row.email || "", Address: row.address || "", OpeningDue: row.openingDue || 0 }); }}>Edit</button> <button className="smallbtn" onClick={() => deleteParty(row)}>Delete</button></> }])
        ]} rows={data} />
      </Panel>
    </div>
    {isCustomer && selected && <div className="two">
      <Panel title={`${selected.customerName} · Due ${money(balance?.currentDue)}`}>
        <div className="stats"><div className="stat"><span>Current Due</span><b>{money(balance?.currentDue)}</b></div><div className="stat"><span>Credit Limit</span><b>{money(balance?.creditLimit)}</b></div><div className="stat"><span>Available Credit</span><b>{money(balance?.availableCredit)}</b></div></div>
        <Table columns={[
          { key: "transactionDate", label: "Date", render: row => new Date(row.transactionDate).toLocaleString("en-BD") },
          { key: "transactionType", label: "Type" },
          { key: "referenceNo", label: "Reference", render: row => row.referenceNo || "-" },
          { key: "debit", label: "Due Added", render: row => money(row.debit) },
          { key: "credit", label: "Paid", render: row => money(row.credit) },
          { key: "balance", label: "Balance", render: row => money(row.balance) },
          { key: "description", label: "Notes" },
          { key: "receipt", label: "Receipt", render: row => row.transactionType === "Payment" ? <button className="smallbtn" onClick={() => downloadPaymentReceipt(row, selected)}>Download</button> : null }
        ]} rows={ledger} />
        <h3>Customer Invoices</h3>
        <Table columns={[
          { key: "invoiceNo", label: "Invoice" },
          { key: "saleDate", label: "Date", render: row => new Date(row.saleDate).toLocaleString("en-BD") },
          { key: "grandTotal", label: "Total", render: row => money(row.grandTotal) },
          { key: "paidAmount", label: "Paid", render: row => money(row.paidAmount) },
          { key: "dueAmount", label: "Due", render: row => money(row.dueAmount) },
          { key: "print", label: "PDF", render: row => <button className="smallbtn" onClick={async () => printInvoice(await sales.get(row.id), { customer: selected })}>Print</button> }
        ]} rows={customerSales} />
      </Panel>
      <Panel title="Receive Customer Payment">
        <form className="formgrid" onSubmit={receivePayment}>
          <Field label="Payment amount" type="number" min="0.01" max={balance?.currentDue || 0} step="0.01" value={payment.amount} onChange={event => setPayment({ ...payment, amount: event.target.value })} required />
          <Select label="Payment method" value={payment.paymentMethod} onChange={event => setPayment({ ...payment, paymentMethod: event.target.value })}><option>Cash</option><option>Card</option><option>bKash</option><option>Nagad</option><option>Rocket</option><option>Bank</option></Select>
          <Field label="Reference number" value={payment.referenceNo} onChange={event => setPayment({ ...payment, referenceNo: event.target.value })} />
          <Select label="Branch" value={payment.branchId} onChange={event => setPayment({ ...payment, branchId: event.target.value })}><option value="">No branch</option>{branches.map(branch => <option value={branch.id} key={branch.id}>{branch.branchName}</option>)}</Select>
          <Field label="Notes" value={payment.notes} onChange={event => setPayment({ ...payment, notes: event.target.value })} />
          <button className="primary" disabled={savingPayment || !balance?.currentDue}>{savingPayment ? "Recording..." : "Record payment"}</button>
        </form>
      </Panel>
    </div>}
  </Page>;
}
