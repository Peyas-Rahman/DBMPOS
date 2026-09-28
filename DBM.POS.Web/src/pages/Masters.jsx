import { useEffect, useState } from "react";
import { master } from "../api";
import { Page, Panel, Table, Field, Select } from "../components/common";

const blank = { Code: "", Name: "", Description: "", ConversionFactor: 1, ParentCategoryId: "" };

export default function Masters() {
  const [type, setType] = useState("categories");
  const [data, setData] = useState([]);
  const [categories, setCategories] = useState([]);
  const [form, setForm] = useState(blank);
  const [message, setMessage] = useState("");

  async function load() {
    const loader = type === "categories" ? master.categories : type === "brands" ? master.brands : master.units;
    const rows = await loader();
    setData(rows);
    if (type === "categories") setCategories(rows);
  }

  useEffect(() => { setMessage(""); load(); }, [type]);

  async function save(event) {
    event.preventDefault();
    try {
      const create = type === "categories" ? master.addCategory : type === "brands" ? master.addBrand : master.addUnit;
      await create(type === "categories" ? { ...form, ParentCategoryId: form.ParentCategoryId || null } : form);
      setForm(blank);
      setMessage("Saved successfully.");
      await load();
    } catch (error) {
      setMessage(error.response?.data?.message || "Could not save record.");
    }
  }

  const categoryName = (row) => row.parentCategoryName ? `${row.parentCategoryName} / ${row.categoryName}` : row.categoryName;
  const columns = type === "categories"
    ? [{ key: "categoryCode", label: "Code" }, { key: "categoryName", label: "Category", render: categoryName }, { key: "parentCategoryName", label: "Parent", render: (row) => row.parentCategoryName || "Top level" }]
    : [{ key: type === "brands" ? "brandCode" : "unitCode", label: "Code" }, { key: type === "brands" ? "brandName" : "unitName", label: "Name" }];

  return <Page title="Master Data" eyebrow="STOCK">
    <div className="tabs">{["categories", "brands", "units"].map(item => <button className={type === item ? "sel" : ""} onClick={() => setType(item)} key={item}>{item}</button>)}</div>
    {message && <div className="notice">{message}</div>}
    <div className="two">
      <Panel title={`New ${type === "categories" ? "Category" : type.slice(0, -1)}`}>
        <form onSubmit={save}>
          <Field label="Code" value={form.Code} onChange={event => setForm(current => ({ ...current, Code: event.target.value }))} required />
          <Field label="Name" value={form.Name} onChange={event => setForm(current => ({ ...current, Name: event.target.value }))} required />
          {type === "categories" && <><Field label="Description" value={form.Description} onChange={event => setForm(current => ({ ...current, Description: event.target.value }))} /><Select label="Parent category" value={form.ParentCategoryId} onChange={event => setForm(current => ({ ...current, ParentCategoryId: event.target.value }))}><option value="">None (top-level category)</option>{categories.filter(category => !category.parentCategoryId).map(category => <option value={category.id} key={category.id}>{category.categoryName}</option>)}</Select></>}
          {type === "units" && <Field label="Conversion Factor" type="number" value={form.ConversionFactor} onChange={event => setForm(current => ({ ...current, ConversionFactor: +event.target.value }))} />}
          <button className="primary">Save</button>
        </form>
      </Panel>
      <Panel title="Records"><Table columns={columns} rows={data} /></Panel>
    </div>
  </Page>;
}