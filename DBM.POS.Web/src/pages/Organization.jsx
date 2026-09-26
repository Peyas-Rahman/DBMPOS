import { useEffect, useState } from "react";
import { org } from "../api";
import { Page, Panel, Table, Field, Select } from "../components/common";

const emptyWarehouse = { branchId: "", code: "", name: "", isDefault: false };

export default function Organization() {
  const [company, setCompany] = useState();
  const [branches, setBranches] = useState([]);
  const [warehouses, setWarehouses] = useState([]);
  const [form, setForm] = useState(emptyWarehouse);
  const [editing, setEditing] = useState(null);
  const [message, setMessage] = useState("");

  async function load() {
    const [branchList, warehouseList] = await Promise.all([org.branches(), org.warehouses()]);
    setBranches(branchList);
    setWarehouses(warehouseList);
    if (!form.branchId && branchList[0]) setForm(current => ({ ...current, branchId: branchList[0].id }));
  }

  useEffect(() => {
    org.company().then(setCompany);
    load().catch(error => setMessage(error.response?.data?.message || "Could not load organization data."));
  }, []);

  function change(key, value) {
    setForm(current => ({ ...current, [key]: value }));
  }

  async function saveWarehouse(event) {
    event.preventDefault();
    setMessage("");
    try {
      if (editing) await org.updateWarehouse(editing.id, form);
      else await org.addWarehouse(form);
      setForm({ ...emptyWarehouse, branchId: form.branchId });
      setEditing(null);
      await load();
      setMessage(editing ? "Warehouse updated successfully." : "Warehouse created successfully.");
    } catch (error) {
      setMessage(error.response?.data?.message || "Warehouse save failed.");
    }
  }

  async function deleteWarehouse(warehouse) {
    if (!window.confirm(`Delete warehouse ${warehouse.warehouseName}?`)) return;
    try {
      await org.deleteWarehouse(warehouse.id);
      await load();
      setMessage("Warehouse deleted successfully.");
    } catch (error) {
      setMessage(error.response?.data?.message || "Warehouse deletion failed.");
    }
  }

  return <Page title="Organization" eyebrow="SETTINGS">
    {message && <div className="notice">{message}<button onClick={() => setMessage("")}>×</button></div>}
    <div className="stats">
      <div className="stat"><span>Company</span><b>{company?.companyName || "-"}</b></div>
      <div className="stat"><span>Business Type</span><b>{company?.businessType || "-"}</b></div>
      <div className="stat"><span>Branches</span><b>{branches.length}</b></div>
      <div className="stat"><span>Warehouses</span><b>{warehouses.length}</b></div>
    </div>
    <div className="two">
      <Panel title={editing ? "Edit Warehouse" : "Add Warehouse"}>
        <form className="formgrid" onSubmit={saveWarehouse}>
          <Select label="Branch" value={form.branchId} onChange={event => change("branchId", event.target.value)} required>
            <option value="">Select branch</option>{branches.map(branch => <option value={branch.id} key={branch.id}>{branch.branchName}</option>)}
          </Select>
          <Field label="Warehouse code" value={form.code} onChange={event => change("code", event.target.value)} required />
          <Field label="Warehouse name" value={form.name} onChange={event => change("name", event.target.value)} required />
          <label className="field"><span>Default warehouse</span><input type="checkbox" checked={form.isDefault} onChange={event => change("isDefault", event.target.checked)} /></label>
          <button className="primary">{editing ? "Update warehouse" : "Create warehouse"}</button>
          {editing && <button type="button" onClick={() => { setEditing(null); setForm({ ...emptyWarehouse, branchId: form.branchId }); }}>Cancel</button>}
        </form>
      </Panel>
      <Panel title="Warehouses">
        <Table columns={[
          { key: "warehouseCode", label: "Code" },
          { key: "warehouseName", label: "Warehouse" },
          { key: "branchId", label: "Branch", render: row => branches.find(branch => branch.id === row.branchId)?.branchName || "-" },
          { key: "isDefault", label: "Default", render: row => row.isDefault ? "Yes" : "No" },
          { key: "actions", label: "Actions", render: row => <><button className="smallbtn" onClick={() => { setEditing(row); setForm({ branchId: row.branchId, code: row.warehouseCode, name: row.warehouseName, isDefault: row.isDefault }); }}>Edit</button> <button className="smallbtn" onClick={() => deleteWarehouse(row)}>Delete</button></> }
        ]} rows={warehouses} />
      </Panel>
    </div>
  </Page>;
}
