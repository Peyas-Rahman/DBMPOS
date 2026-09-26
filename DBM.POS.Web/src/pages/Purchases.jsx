import { useEffect, useState } from "react";
import { purchases, org, suppliers, products } from "../api";
import { Page, Panel, Table, Field, Select } from "../components/common";
import { money } from "../utils";

const emptyItem = { productId: "", quantity: 1, unitCost: 0 };

export default function Purchases() {
  const [history, setHistory] = useState([]);
  const [supplierList, setSupplierList] = useState([]);
  const [branches, setBranches] = useState([]);
  const [warehouses, setWarehouses] = useState([]);
  const [productList, setProductList] = useState([]);
  const [form, setForm] = useState({ branchId: "", warehouseId: "", supplierId: "", paidAmount: 0, items: [] });
  const [item, setItem] = useState(emptyItem);
  const [message, setMessage] = useState("");

  useEffect(() => {
    purchases.list().then(setHistory);
    suppliers.list().then(setSupplierList);
    products.list("").then(setProductList);
    org.branches().then(items => {
      setBranches(items);
      if (items[0]) {
        setForm(current => ({ ...current, branchId: items[0].id }));
        org.warehouses(items[0].id).then(setWarehouses);
      }
    });
  }, []);

  const subtotal = form.items.reduce((sum, line) => sum + Number(line.quantity || 0) * Number(line.unitCost || 0), 0);
  const paidAmount = Number(form.paidAmount || 0);
  const dueAmount = Math.max(0, subtotal - paidAmount);

  function addItem() {
    if (!item.productId || Number(item.quantity) <= 0) return setMessage("Select a product and enter a valid quantity.");
    setForm(current => ({ ...current, items: [...current.items, { ...item, quantity: Number(item.quantity), unitCost: Number(item.unitCost) }] }));
    setItem(emptyItem);
    setMessage("");
  }

  function removeItem(index) {
    setForm(current => ({ ...current, items: current.items.filter((_, itemIndex) => itemIndex !== index) }));
  }

  async function savePurchase() {
    if (!form.items.length) return setMessage("Add at least one product.");
    if (paidAmount > subtotal) return setMessage("Paid amount cannot exceed purchase total.");
    try {
      await purchases.create(form);
      setForm(current => ({ ...current, items: [], paidAmount: 0 }));
      setHistory(await purchases.list());
      setMessage("Purchase saved successfully.");
    } catch (error) {
      setMessage(error.response?.data?.message || "Purchase failed.");
    }
  }

  return <Page title="Purchase Orders" eyebrow="STOCK / PURCHASE">
    {message && <div className="notice">{message}<button onClick={() => setMessage("")}>×</button></div>}
    <div className="two">
      <Panel title="New Purchase">
        <Select label="Branch" value={form.branchId} onChange={event => { const branchId = event.target.value; setForm({ ...form, branchId, warehouseId: "" }); org.warehouses(branchId).then(setWarehouses); }} required>
          <option value="">Select</option>{branches.map(branch => <option value={branch.id} key={branch.id}>{branch.branchName}</option>)}
        </Select>
        <Select label="Warehouse" value={form.warehouseId} onChange={event => setForm({ ...form, warehouseId: event.target.value })} required>
          <option value="">Select</option>{warehouses.map(warehouse => <option value={warehouse.id} key={warehouse.id}>{warehouse.warehouseName}</option>)}
        </Select>
        <Select label="Supplier" value={form.supplierId} onChange={event => setForm({ ...form, supplierId: event.target.value })} required>
          <option value="">Select</option>{supplierList.map(supplier => <option value={supplier.id} key={supplier.id}>{supplier.supplierName}</option>)}
        </Select>
        <div className="addline">
          <select value={item.productId} onChange={event => setItem({ ...item, productId: event.target.value })}><option value="">Product</option>{productList.map(product => <option value={product.id} key={product.id}>{product.productName}</option>)}</select>
          <input type="number" min="1" step="0.01" value={item.quantity} onChange={event => setItem({ ...item, quantity: event.target.value })} placeholder="Qty" />
          <input type="number" min="0" step="0.01" value={item.unitCost} onChange={event => setItem({ ...item, unitCost: event.target.value })} placeholder="Unit cost" />
          <button type="button" onClick={addItem}>Add</button>
        </div>
        {form.items.map((line, index) => <div className="line" key={`${line.productId}-${index}`}><span>{productList.find(product => product.id === line.productId)?.productName || "Product"} × {line.quantity} @ {money(line.unitCost)}</span><strong>{money(Number(line.quantity) * Number(line.unitCost))}</strong><button type="button" className="smallbtn" onClick={() => removeItem(index)}>×</button></div>)}
        <div className="sum"><span>Purchase total</span><b>{money(subtotal)}</b></div>
        <Field label="Paid Amount" type="number" min="0" max={subtotal} step="0.01" value={form.paidAmount} onChange={event => setForm({ ...form, paidAmount: event.target.value })} />
        <div className="sum"><span>Due</span><b>{money(dueAmount)}</b></div>
        <button className="primary" onClick={savePurchase}>Save Purchase</button>
      </Panel>
      <Panel title="Purchase History">
        <Table columns={[{ key: "purchaseNo", label: "Purchase" }, { key: "purchaseDate", label: "Date" }, { key: "grandTotal", label: "Total", render: row => money(row.grandTotal) }, { key: "paidAmount", label: "Paid", render: row => money(row.paidAmount) }, { key: "dueAmount", label: "Due", render: row => money(row.dueAmount) }]} rows={history} />
      </Panel>
    </div>
  </Page>;
}
