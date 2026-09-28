import { useEffect, useState } from "react";
import { master, products } from "../api";
import { money } from "../utils";
import { Page, Panel, Table, Field, Select } from "../components/common";

const empty = { productCode: "", productName: "", barcode: "", description: "", categoryId: "", brandId: "", unitId: "", costPrice: 0, salePrice: 0, MRP: 0, taxPercent: 0, minStockLevel: 0, trackBatch: false, trackExpiry: false, allowNegativeStock: false };
const splitValues = values => [...new Set((values || "").split(",").map(value => value.trim()).filter(Boolean))];
const productCodePrefix = name => name.normalize("NFKD").replace(/[\u0300-\u036f]/g, "").toUpperCase().replace(/[^A-Z0-9]+/g, "-").replace(/^-|-$/g, "").slice(0, 12).replace(/-+$/g, "") || "ITEM";

function combinations(groups) {
  return groups.reduce((results, group) => results.flatMap(result => group.values.map(value => [...result, { attribute: group.attribute, value }])), [[]]);
}

export default function Products() {
  const [data, setData] = useState([]);
  const [cats, setCats] = useState([]);
  const [brands, setBrands] = useState([]);
  const [units, setUnits] = useState([]);
  const [attributes, setAttributes] = useState([]);
  const [form, setForm] = useState(empty);
  const [attributeOptions, setAttributeOptions] = useState({});
  const [edit, setEdit] = useState(null);
  const [q, setQ] = useState("");
  const [msg, setMsg] = useState("");

  async function load() { setData(await products.list(q)); }

  useEffect(() => {
    load();
    master.categories().then(setCats);
    master.brands().then(setBrands);
    master.units().then(setUnits);
    master.productAttributes().then(items => {
      setAttributes(items);
      setAttributeOptions(Object.fromEntries(items.map(item => [item.id, { enabled: false, values: splitValues(item.defaultValues), custom: "" }])));
    });
  }, []);

  const f = (key, value) => setForm(current => ({ ...current, [key]: value }));
  function suggestedCode(name) {
    const prefix = productCodePrefix(name);
    const used = new Set(data.map(product => product.productCode).filter(code => code.startsWith(`${prefix}-`)).map(code => Number(code.slice(prefix.length + 1))).filter(Number.isInteger));
    let next = 1;
    while (used.has(next)) next++;
    return `${prefix}-${String(next).padStart(3, "0")}`;
  }
  const activeGroups = attributes.flatMap(attribute => {
    const selection = attributeOptions[attribute.id];
    if (!selection?.enabled) return [];
    const values = [...new Set([...selection.values, ...splitValues(selection.custom)])];
    return values.length ? [{ attribute, values }] : [];
  });

  async function save(event) {
    event.preventDefault();
    try {
      const saved = edit ? await products.update(edit, form) : await products.create({ ...form, productCode: "" });
      const generatedCode = saved.productCode || saved.ProductCode || form.productCode;
      const existingVariants = edit ? (data.find(product => product.id === edit)?.variants || []) : [];
      const existingNames = new Set(existingVariants.map(variant => variant.variantName));
      const variantsToCreate = combinations(activeGroups).map(values => ({
        name: values.map(item => `${item.attribute.attributeName}: ${item.value}`).join(" / "),
        values
      })).filter(variant => !existingNames.has(variant.name));
      let variantNumber = existingVariants.length + 1;
      for (const variant of variantsToCreate) {
        const suffix = String(variantNumber++).padStart(3, "0");
        await products.addVariant(saved.id || edit, {
          variantCode: `${generatedCode}-V${suffix}`,
          variantName: variant.name,
          barcode: null,
          sku: `${generatedCode}-V${suffix}`,
          costPrice: Number(form.costPrice),
          salePrice: Number(form.salePrice),
          MRP: Number(form.MRP),
          attributes: variant.values.map(item => ({ productAttributeId: item.attribute.id, value: item.value }))
        });
      }
      setForm({ ...empty, productCode: generatedCode });
      setEdit(null);
      setMsg(variantsToCreate.length ? `Saved with product code ${generatedCode}; ${variantsToCreate.length} variants created.` : `Saved with product code ${generatedCode}.`);
      await load();
    } catch (error) {
      setMsg(error.response?.data?.message || "Save failed");
    }
  }

  function startNew() {
    setEdit(null);
    setForm(empty);
    setAttributeOptions(current => Object.fromEntries(attributes.map(attribute => [attribute.id, { enabled: false, values: splitValues(attribute.defaultValues), custom: "" }])));
  }

  function startEdit(product) {
    setEdit(product.id);
    setForm({ ...empty, ...product, categoryId: product.categoryId || "", brandId: product.brandId || "", unitId: product.unitId || "" });
    const options = Object.fromEntries(attributes.map(attribute => [attribute.id, { enabled: false, values: splitValues(attribute.defaultValues), custom: "" }]));
    for (const variant of product.variants || []) {
      for (const value of variant.attributes || []) {
        const selection = options[value.productAttributeId];
        if (selection) {
          selection.enabled = true;
          if (!selection.values.includes(value.value)) selection.values.push(value.value);
        }
      }
    }
    setAttributeOptions(options);
  }

  function updateAttribute(attribute, update) {
    setAttributeOptions(current => ({ ...current, [attribute.id]: { ...current[attribute.id], ...update } }));
  }

  return <Page title="Products" eyebrow="STOCK / ITEMS MANAGER" action={<button className="primary" onClick={startNew}>+ New Product</button>}>
    <div className="toolbar"><input placeholder="Search products..." value={q} onChange={event => setQ(event.target.value)} onKeyDown={event => event.key === "Enter" && load()} /><button onClick={load}>Search</button></div>
    {msg && <div className="notice">{msg}</div>}
    <div className="two">
      <Panel title={edit ? "Edit Product" : "Product Form"}>
        <form className="formgrid" onSubmit={save}>
          <Field label="Product Code" value={form.productCode} onChange={event => f("productCode", event.target.value)} placeholder={edit ? "Product code" : "Auto-generated from product name"} readOnly={!edit} />
          <Field label="Product Name" value={form.productName} onChange={event => { const name = event.target.value; setForm(current => ({ ...current, productName: name, ...(!edit ? { productCode: suggestedCode(name) } : {}) })); }} required />
          <Field label="Barcode" value={form.barcode} onChange={event => f("barcode", event.target.value)} />
          <Select label="Category" value={form.categoryId} onChange={event => f("categoryId", event.target.value)}><option value="">Select</option>{cats.map(item => <option value={item.id} key={item.id}>{categoryLabel(item)}</option>)}</Select>
          <Select label="Brand" value={form.brandId} onChange={event => f("brandId", event.target.value)}><option value="">Select</option>{brands.map(item => <option value={item.id} key={item.id}>{item.brandName}</option>)}</Select>
          <Select label="Unit" value={form.unitId} onChange={event => f("unitId", event.target.value)}><option value="">Select</option>{units.map(item => <option value={item.id} key={item.id}>{item.unitName}</option>)}</Select>
          <Field label="Cost Price" type="number" value={form.costPrice} onChange={event => f("costPrice", +event.target.value)} />
          <Field label="Sale Price" type="number" value={form.salePrice} onChange={event => f("salePrice", +event.target.value)} />
          <Field label="MRP" type="number" value={form.MRP} onChange={event => f("MRP", +event.target.value)} />
          <Field label="Tax %" type="number" value={form.taxPercent} onChange={event => f("taxPercent", +event.target.value)} />
          <Field label="Minimum Stock" type="number" value={form.minStockLevel} onChange={event => f("minStockLevel", +event.target.value)} />
          <label className="check"><input type="checkbox" checked={form.trackBatch} onChange={event => f("trackBatch", event.target.checked)} /> Track Batch</label>
          <label className="check"><input type="checkbox" checked={form.trackExpiry} onChange={event => f("trackExpiry", event.target.checked)} /> Track Expiry</label>
          {attributes.length > 0 && <fieldset className="size-options"><legend>Variant attributes</legend>{attributes.map(attribute => {
            const selection = attributeOptions[attribute.id] || { enabled: false, values: splitValues(attribute.defaultValues), custom: "" };
            const options = [...new Set([...splitValues(attribute.defaultValues), ...splitValues(selection.custom)])];
            return <div className="attribute-group" key={attribute.id}><label className="check"><input type="checkbox" checked={selection.enabled} onChange={event => updateAttribute(attribute, { enabled: event.target.checked })} /> {attribute.attributeName}</label>{selection.enabled && <><div className="attribute-values">{options.map(value => <label className="check" key={value}><input type="checkbox" checked={selection.values.includes(value)} onChange={event => updateAttribute(attribute, { values: event.target.checked ? [...selection.values, value] : selection.values.filter(item => item !== value) })} /> {value}</label>)}</div><input aria-label={`Extra ${attribute.attributeName} options`} value={selection.custom} onChange={event => updateAttribute(attribute, { custom: event.target.value })} placeholder="Extra options, comma separated" /></>}</div>;
          })}<small>Manage available attributes and default options from Product Attributes.</small></fieldset>}
          <button className="primary" type="submit">Save Product</button>
        </form>
      </Panel>
      <Panel title="Product List"><Table columns={[{ key: "productCode", label: "Code" }, { key: "productName", label: "Product" }, { key: "category", label: "Category" }, { key: "salePrice", label: "Sale Price", render: row => money(row.salePrice) }, { key: "stock", label: "Actions", render: row => <button className="smallbtn" onClick={() => startEdit(row)}>Edit</button> }]} rows={data} /></Panel>
    </div>
  </Page>;
}

const categoryLabel = category => category.parentCategoryName ? `${category.parentCategoryName} / ${category.categoryName}` : category.categoryName;