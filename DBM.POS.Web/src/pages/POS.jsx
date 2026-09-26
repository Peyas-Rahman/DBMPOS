import { useEffect, useMemo, useRef, useState } from "react";
import { auth, products, org, sales, customers as customerApi } from "../api";
import { money } from "../utils";
import { downloadInvoicePdf, printInvoice } from "../utils/printInvoice";
import { Page } from "../components/common";
import { useNavigate } from "react-router-dom";

export default function POS({ fullScreen = false }) {
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const [productList, setProductList] = useState([]);
  const [cart, setCart] = useState([]);
  const [branches, setBranches] = useState([]);
  const [warehouses, setWarehouses] = useState([]);
  const [customers, setCustomers] = useState([]);
  const [customerSearch, setCustomerSearch] = useState("");
  const [branchId, setBranchId] = useState("");
  const [warehouseId, setWarehouseId] = useState("");
  const [customerId, setCustomerId] = useState("");
  const [discount, setDiscount] = useState(0);
  const [discountMode, setDiscountMode] = useState("fixed");
  const [paid, setPaid] = useState(0);
  const [method, setMethod] = useState("Cash");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [receipt, setReceipt] = useState(null);
  const [customerModal, setCustomerModal] = useState(false);
  const [customerForm, setCustomerForm] = useState({ Code: "", Name: "", Phone: "", Email: "", Address: "", OpeningDue: 0, DiscountPercent: 0 });
  const [customerError, setCustomerError] = useState("");
  const [savingCustomer, setSavingCustomer] = useState(false);
  const [currentUser, setCurrentUser] = useState(null);
  const input = useRef();

  useEffect(() => {
    products.list("").then(setProductList);
    auth.me().then(setCurrentUser).catch(() => {});
    org.branches().then(items => {
      setBranches(items);
      if (items[0]) setBranchId(items[0].id);
    });
  }, []);

  useEffect(() => {
    let active = true;
    const timer = setTimeout(() => {
      customerApi.list(customerSearch.trim())
        .then(items => {
          if (active) setCustomers(items);
        })
        .catch(() => {
          if (active) setMessage("Could not search customers.");
        });
    }, 250);
    return () => {
      active = false;
      clearTimeout(timer);
    };
  }, [customerSearch]);

  useEffect(() => {
    if (!branchId) return;
    org.warehouses(branchId).then(items => {
      setWarehouses(items);
      setWarehouseId(items[0]?.id || "");
    });
  }, [branchId]);

  const filteredProducts = useMemo(() => productList.slice(0, 50), [productList]);
  const subtotal = cart.reduce((sum, item) => sum + item.qty * item.price, 0);
  const manualDiscount = Math.min(subtotal, discountMode === "percent"
    ? subtotal * Math.max(0, Number(discount || 0)) / 100
    : Math.max(0, Number(discount || 0)));
  const selectedCustomer = customers.find(item => item.id === customerId) || null;
  const membershipDiscount = Math.min(subtotal - manualDiscount, subtotal * Math.max(0, Number(selectedCustomer?.discountPercent || 0)) / 100);
  const totalDiscount = manualDiscount + membershipDiscount;
  const total = Math.max(0, subtotal - totalDiscount);
  const tendered = Math.max(0, Number(paid || 0));
  const appliedPayment = Math.min(total, tendered);
  const due = Math.max(0, total - appliedPayment);
  const change = method === "Cash" ? Math.max(0, tendered - total) : 0;
  const matchingCustomers = customers;

  function openCustomerModal() {
    setCustomerForm({ Code: `CUS-${Date.now()}`, Name: "", Phone: "", Email: "", Address: "", OpeningDue: 0, DiscountPercent: 0 });
    setCustomerError("");
    setCustomerModal(true);
  }

  async function createCustomer(event) {
    event.preventDefault();
    setSavingCustomer(true);
    setCustomerError("");
    try {
      const created = await customerApi.create(customerForm);
      setCustomers(current => [...current, created].sort((a, b) => a.customerName.localeCompare(b.customerName)));
      setCustomerId(created.id);
      setCustomerSearch("");
      setCustomerModal(false);
    } catch (error) {
      setCustomerError(error.response?.data?.message || "Could not create customer.");
    } finally {
      setSavingCustomer(false);
    }
  }

  async function searchProducts(value) {
    setQuery(value);
    try {
      setProductList(await products.list(value));
    } catch {
      setMessage("Could not search products.");
    }
  }

  async function searchCustomers(value, selectFirst = false) {
    try {
      const items = await customerApi.list(value.trim());
      setCustomers(items);
      if (selectFirst && items.length === 1) setCustomerId(items[0].id);
      if (selectFirst && !items.length) setMessage("No customer found.");
    } catch {
      setMessage("Could not search customers.");
    }
  }

  function addProduct(product, variant) {
    const key = variant?.id || product.id;
    setCart(current => {
      const found = current.find(item => item.key === key);
      if (found) return current.map(item => item.key === key ? { ...item, qty: item.qty + 1 } : item);
      return [...current, {
        key,
        productId: product.id,
        variantId: variant?.id || null,
        name: variant ? `${product.productName} / ${variant.variantName}` : product.productName,
        code: variant?.variantCode || product.productCode,
        price: Number(variant?.salePrice ?? product.salePrice),
        cost: Number(variant?.costPrice ?? product.costPrice),
        qty: 1
      }];
    });
  }

  async function scanBarcode() {
    if (!query.trim()) return;
    try {
      const result = await products.barcode(query.trim());
      if (result.type === "Product") addProduct(result.product);
      else addProduct(result.variant.product, result.variant);
      setQuery("");
    } catch {
      setMessage("Barcode not found.");
    }
  }

  async function checkout() {
    if (!branchId || !warehouseId) return setMessage("Select branch and warehouse.");
    if (!cart.length) return setMessage("Cart is empty.");
    if (method !== "Cash" && tendered > total) return setMessage("Paid amount cannot exceed total for non-cash payments.");
    setBusy(true);
    setMessage("");
    try {
      const saleItems = cart.map(item => ({
        productId: item.productId,
        variantId: item.variantId,
        quantity: item.qty,
        unitPrice: item.price,
        unitCost: item.cost,
        discount: Number((totalDiscount * (item.qty * item.price) / (subtotal || 1)).toFixed(2)),
        tax: 0
      }));
      const result = await sales.create({
        branchId,
        warehouseId,
        customerId: customerId || null,
        posDeviceId: null,
        invoiceNo: null,
        paidAmount: appliedPayment,
        items: saleItems,
        payments: appliedPayment > 0 ? [{ method, amount: appliedPayment, referenceNo: null }] : []
      });
      setReceipt({
        ...result,
        invoiceNo: result.invoiceNo || result.InvoiceNo,
        items: cart.map((item, index) => ({
          name: item.name,
          quantity: item.qty,
          unitPrice: item.price,
          discount: saleItems[index].discount,
          lineTotal: item.qty * item.price - saleItems[index].discount
        })),
        customer: selectedCustomer,
        changeAmount: change
      });
      setMessage(`Sale completed: ${result.invoiceNo}`);
      setCart([]);
      setDiscount(0);
      setPaid(0);
    } catch (error) {
      setMessage(error.response?.data?.message || "Sale failed.");
    } finally {
      setBusy(false);
    }
  }

  function printCurrentInvoice() {
    if (!receipt) return;
    printInvoice(receipt, {
      customer: receipt.customer,
      soldBy: currentUser?.fullName,
      branchName: branches.find(item => item.id === branchId)?.branchName || "",
      warehouseName: warehouses.find(item => item.id === warehouseId)?.warehouseName || ""
    });
  }

  return <div style={fullScreen ? { minHeight: "100vh", padding: "16px", background: "#f4f7fb" } : undefined}><Page title="New Invoice" eyebrow="POINT OF SALE" action={fullScreen ? <button className="pos-home" title="Back to Dashboard" aria-label="Back to Dashboard" onClick={() => navigate("/")}>⌂</button> : <span className="online">API Connected</span>}>
    {message && <div className="notice">{message}<button onClick={() => setMessage("")}>×</button></div>}
    <div className="pos">
      <section className="panel">
        <div className="posfilters">
          <select value={branchId} onChange={event => setBranchId(event.target.value)}><option value="">Branch</option>{branches.map(item => <option key={item.id} value={item.id}>{item.branchName}</option>)}</select>
          <select value={warehouseId} onChange={event => setWarehouseId(event.target.value)}><option value="">Warehouse</option>{warehouses.map(item => <option key={item.id} value={item.id}>{item.warehouseName}</option>)}</select>
          <input ref={input} value={query} onChange={event => searchProducts(event.target.value)} onKeyDown={event => event.key === "Enter" && scanBarcode()} placeholder="Search product / barcode / SKU" />
          <button onClick={scanBarcode}>Scan</button>
        </div>
        <div className="productgrid">{filteredProducts.map(product => <button className="pitem" key={product.id} onClick={() => addProduct(product)}><small>{product.productCode}</small><b>{product.productName}</b><span>{product.brand || "No brand"} · {product.unit || "Unit"}</span><strong>{money(product.salePrice)}</strong>{product.variants?.length > 0 && <em>{product.variants.length} variants</em>}</button>)}</div>
      </section>
      <section className="panel cart">
        <div className="paneltitle"><h2>Cart</h2><button className="textbtn" onClick={() => setCart([])}>Clear</button></div>
        <div className="cartitems">{cart.map(item => <div className="cartitem" key={item.key}><div><b>{item.name}</b><small>{item.code} · {money(item.price)}</small></div><input type="number" min="1" value={item.qty} onChange={event => setCart(current => current.map(line => line.key === item.key ? { ...line, qty: event.target.value === "" ? "" : Math.max(1, Number(event.target.value)) } : line))} onBlur={() => setCart(current => current.map(line => line.key === item.key ? { ...line, qty: Math.max(1, Number(line.qty) || 1) } : line))} /><strong>{money(item.qty * item.price)}</strong><button onClick={() => setCart(current => current.filter(line => line.key !== item.key))}>×</button></div>)}</div>
        <div className="checkout">
          <div className="customer-field"><span>Customer</span><div style={{ display: "flex", gap: "6px", alignItems: "center", width: "100%" }}><button type="button" className="textbtn" style={{ flex: "0 0 auto", whiteSpace: "nowrap", height: "36px", margin: 0 }} onClick={openCustomerModal}>+ Add customer</button><input style={{ flex: "1 1 auto", minWidth: 0, height: "36px", margin: 0 }} value={customerSearch} onChange={event => setCustomerSearch(event.target.value)} onKeyDown={event => { if (event.key === "Enter") { event.preventDefault(); searchCustomers(customerSearch, true); } }} placeholder="Search customer by phone or name" /></div><select value={customerId} onChange={event => setCustomerId(event.target.value)}><option value="">Walk-in Customer</option>{matchingCustomers.map(item => <option value={item.id} key={item.id}>{item.customerName} {item.phone ? `(${item.phone})` : ""}</option>)}</select></div>
          {selectedCustomer && <div className="customer-summary"><strong>{selectedCustomer.customerName}</strong>{selectedCustomer.phone && <span>{selectedCustomer.phone}</span>}{selectedCustomer.address && <span>{selectedCustomer.address}</span>}</div>}
          <div className="sum"><span>Subtotal</span><b>{money(subtotal)}</b></div>
          {selectedCustomer?.discountPercent > 0 && <div className="sum"><span>Membership discount ({selectedCustomer.discountPercent}%)</span><b>-{money(membershipDiscount)}</b></div>}
          <label>Discount<div style={{ display: "grid", gridTemplateColumns: "minmax(92px, 0.8fr) minmax(0, 1.2fr)", gap: "6px" }}><select value={discountMode} onChange={event => setDiscountMode(event.target.value)} style={{ minWidth: 0, fontSize: "11px", padding: "0 6px" }}><option value="fixed">Fixed</option><option value="percent">Percent</option></select><input type="number" min="0" max={discountMode === "percent" ? 100 : undefined} step="0.01" value={discount} onChange={event => setDiscount(event.target.value)} placeholder={discountMode === "percent" ? "Discount %" : "Discount amount"} /></div></label>
          {totalDiscount > 0 && <div className="sum"><span>Total discount</span><b>-{money(totalDiscount)}</b></div>}
          <div className="sum total"><span>Grand Total</span><b>{money(total)}</b></div>
          <div className="paybuttons">{["Cash", "Card", "bKash", "Nagad", "Rocket", "Credit"].map(payment => <button className={method === payment ? "selected" : ""} onClick={() => setMethod(payment)} key={payment}>{payment}</button>)}</div>
          <label>{method === "Cash" ? "Cash received" : "Paid amount"}<input type="number" min="0" step="0.01" value={paid} onChange={event => setPaid(event.target.value)} /></label>
          {change > 0 && <div className="sum"><span>Return change</span><b>{money(change)}</b></div>}
          <div className="sum"><span>Due</span><b className={due ? "danger" : ""}>{money(due)}</b></div>
          <button className="complete" disabled={busy || !cart.length} onClick={checkout}>{busy ? "Processing..." : `Complete Sale · ${money(total)}`}</button>
        </div>
      </section>
      {receipt && <section className="panel receipt"><div className="paneltitle"><h2>Invoice {receipt.invoiceNo || receipt.InvoiceNo}</h2><div><button className="smallbtn" onClick={() => downloadInvoicePdf(receipt, { customer: receipt.customer, soldBy: currentUser?.fullName, branchName: branches.find(item => item.id === branchId)?.branchName || "", warehouseName: warehouses.find(item => item.id === warehouseId)?.warehouseName || "" })}>Download PDF</button> <button className="smallbtn" onClick={printCurrentInvoice}>Print</button></div></div><div className="receiptitems"><strong>{receipt.customer?.customerName || "Walk-in Customer"}</strong><p>Sold by {currentUser?.fullName || "-"} · Total {money(receipt.grandTotal)} · Paid {money(receipt.paidAmount)} · Due {money(receipt.dueAmount)}{receipt.changeAmount > 0 ? ` · Change ${money(receipt.changeAmount)}` : ""}</p></div></section>}
    </div>
    {customerModal && <div className="modal" onMouseDown={event => event.target === event.currentTarget && setCustomerModal(false)}><div className="modalbox" role="dialog" aria-modal="true" aria-labelledby="new-customer-title"><button type="button" className="close" aria-label="Close" onClick={() => setCustomerModal(false)}>×</button><h2 id="new-customer-title">Add New Customer</h2>{customerError && <div className="error">{customerError}</div>}<form className="formgrid" onSubmit={createCustomer}><label className="field"><span>Customer code</span><input value={customerForm.Code} onChange={event => setCustomerForm({ ...customerForm, Code: event.target.value })} required /></label><label className="field"><span>Customer name</span><input autoFocus value={customerForm.Name} onChange={event => setCustomerForm({ ...customerForm, Name: event.target.value })} required /></label><label className="field"><span>Phone</span><input value={customerForm.Phone} onChange={event => setCustomerForm({ ...customerForm, Phone: event.target.value })} /></label><label className="field"><span>Email</span><input type="email" value={customerForm.Email} onChange={event => setCustomerForm({ ...customerForm, Email: event.target.value })} /></label><label className="field"><span>Address</span><input value={customerForm.Address} onChange={event => setCustomerForm({ ...customerForm, Address: event.target.value })} /></label><label className="field"><span>Opening due</span><input type="number" min="0" step="0.01" value={customerForm.OpeningDue} onChange={event => setCustomerForm({ ...customerForm, OpeningDue: Number(event.target.value) })} /></label><label className="field"><span>Membership discount %</span><input type="number" min="0" max="100" step="0.01" value={customerForm.DiscountPercent} onChange={event => setCustomerForm({ ...customerForm, DiscountPercent: Number(event.target.value) })} /></label><button className="primary" disabled={savingCustomer}>{savingCustomer ? "Saving..." : "Save and select customer"}</button></form></div></div>}
  </Page></div>;
}
