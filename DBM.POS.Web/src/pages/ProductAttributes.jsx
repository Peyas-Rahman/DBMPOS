import { useEffect, useState } from "react";
import { master } from "../api";
import { Page, Panel, Table, Field } from "../components/common";

const blank = { Code: "", Name: "", DefaultValues: "" };

export default function ProductAttributes() {
  const [attributes, setAttributes] = useState([]);
  const [form, setForm] = useState(blank);
  const [editing, setEditing] = useState(null);
  const [message, setMessage] = useState("");

  async function load() {
    setAttributes(await master.productAttributes());
  }

  useEffect(() => { load(); }, []);

  async function save(event) {
    event.preventDefault();
    try {
      if (editing) await master.updateProductAttribute(editing, form);
      else await master.addProductAttribute(form);
      setForm(blank);
      setEditing(null);
      setMessage("Attribute saved.");
      await load();
    } catch (error) {
      setMessage(error.response?.data?.message || "Could not save attribute.");
    }
  }

  function edit(attribute) {
    setEditing(attribute.id);
    setForm({ Code: attribute.attributeCode, Name: attribute.attributeName, DefaultValues: attribute.defaultValues || "" });
  }

  return <Page title="Product Attributes" eyebrow="PRODUCT SETUP">
    {message && <div className="notice">{message}</div>}
    <div className="two">
      <Panel title={editing ? "Edit Attribute" : "New Attribute"}>
        <form onSubmit={save}>
          <Field label="Attribute code" value={form.Code} onChange={event => setForm(current => ({ ...current, Code: event.target.value }))} placeholder="SIZE" required />
          <Field label="Attribute name" value={form.Name} onChange={event => setForm(current => ({ ...current, Name: event.target.value }))} placeholder="Size" required />
          <label className="field"><span>Default options (comma separated)</span><textarea rows="3" value={form.DefaultValues} onChange={event => setForm(current => ({ ...current, DefaultValues: event.target.value }))} placeholder="S, M, L, XL, XXL" /></label>
          <button className="primary" type="submit">{editing ? "Update Attribute" : "Save Attribute"}</button>{editing && <button type="button" onClick={() => { setEditing(null); setForm(blank); }}>Cancel</button>}
        </form>
      </Panel>
      <Panel title="Configured Attributes"><Table columns={[{ key: "attributeCode", label: "Code" }, { key: "attributeName", label: "Attribute" }, { key: "defaultValues", label: "Options" }, { key: "actions", label: "Actions", render: row => <button className="smallbtn" onClick={() => edit(row)}>Edit</button> }]} rows={attributes} /></Panel>
    </div>
  </Page>;
}
