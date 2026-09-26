import { useEffect, useState } from "react";
import { org } from "../api";
import { Page, Panel, Table, Field } from "../components/common";

const emptyForm = { Code: "", Name: "", Phone: "", Address: "", IsHeadOffice: false };

export default function Branches() {
  const [branches, setBranches] = useState([]);
  const [form, setForm] = useState(emptyForm);
  const [editing, setEditing] = useState(null);
  const [message, setMessage] = useState("");

  async function loadBranches() {
    setBranches(await org.branches());
  }

  useEffect(() => {
    loadBranches().catch(error => setMessage(error.response?.data?.message || "Could not load branches."));
  }, []);

  async function saveBranch(event) {
    event.preventDefault();
    setMessage("");
    try {
      if (editing) await org.updateBranch(editing.id, form);
      else await org.addBranch(form);
      setForm(emptyForm);
      setEditing(null);
      await loadBranches();
      setMessage(editing ? "Branch updated successfully." : "Branch created successfully.");
    } catch (error) {
      setMessage(error.response?.data?.message || "Branch creation failed.");
    }
  }

  async function deleteBranch(branch) {
    if (!window.confirm(`Delete branch ${branch.branchName}?`)) return;
    try { await org.deleteBranch(branch.id); await loadBranches(); setMessage("Branch deleted successfully."); }
    catch (error) { setMessage(error.response?.data?.message || "Branch deletion failed."); }
  }

  return <Page title="Branches" eyebrow="ORGANIZATION">
    {message && <div className="notice">{message}<button onClick={() => setMessage("")}>×</button></div>}
    <div className="two">
      <Panel title={editing ? "Edit Branch" : "Add Branch"}>
        <form className="formgrid" onSubmit={saveBranch}>
          <Field label="Branch code" value={form.Code} onChange={event => setForm({ ...form, Code: event.target.value })} required />
          <Field label="Branch name" value={form.Name} onChange={event => setForm({ ...form, Name: event.target.value })} required />
          <Field label="Phone" value={form.Phone} onChange={event => setForm({ ...form, Phone: event.target.value })} />
          <Field label="Address" value={form.Address} onChange={event => setForm({ ...form, Address: event.target.value })} />
          <label className="field"><span>Head office</span><input type="checkbox" checked={form.IsHeadOffice} onChange={event => setForm({ ...form, IsHeadOffice: event.target.checked })} /></label>
          <button className="primary">{editing ? "Update branch" : "Create branch"}</button>
          {editing && <button type="button" onClick={() => { setEditing(null); setForm(emptyForm); }}>Cancel</button>}
        </form>
      </Panel>
      <Panel title="Branch List">
        <Table columns={[{ key: "branchCode", label: "Code" }, { key: "branchName", label: "Branch" }, { key: "phone", label: "Phone" }, { key: "address", label: "Address" }, { key: "actions", label: "Actions", render: row => <><button className="smallbtn" onClick={() => { setEditing(row); setForm({ Code: row.branchCode, Name: row.branchName, Phone: row.phone || "", Address: row.address || "", IsHeadOffice: row.isHeadOffice }); }}>Edit</button> <button className="smallbtn" onClick={() => deleteBranch(row)}>Delete</button></> }]} rows={branches} />
      </Panel>
    </div>
  </Page>;
}