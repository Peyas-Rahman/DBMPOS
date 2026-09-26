import { useState } from "react";
import { useEffect } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import { menu } from "../../config/menu";
import { customers, products, sales } from "../../api";

function Item({ item, level = 0, openPath, setOpenPath, parentPath = [] }) {
  if (!item.children) {
    return <NavLink className={({ isActive }) => `nav ${isActive ? "active" : ""}`} to={item.path}>
      {level === 0 && <i>{item.icon}</i>}
      <span>{item.label}</span>
    </NavLink>;
  }

  const isOpen = openPath[level] === item.label;
  const nextPath = [...parentPath, item.label];

  return <div>
    <button className="nav navbtn" aria-expanded={isOpen} onClick={() => setOpenPath(current => isOpen ? current.slice(0, level) : [...current.slice(0, level), item.label])}>
      {level === 0 && <i>{item.icon}</i>}
      <span>{item.label}</span>
      <em>{isOpen ? "⌄" : "›"}</em>
    </button>
    {isOpen && <div className="sub">{item.children.map((child, index) => <Item key={`${nextPath.join("/")}/${index}`} item={child} level={level + 1} parentPath={nextPath} openPath={openPath} setOpenPath={setOpenPath} />)}</div>}
  </div>;
}

export default function AppShell({ children }) {
  const [mini, setMini] = useState(false);
  const [openPath, setOpenPath] = useState([]);
  const [globalQuery, setGlobalQuery] = useState("");
  const [searchResults, setSearchResults] = useState([]);
  const navigate = useNavigate();
  const user = JSON.parse(localStorage.getItem("user") || "{}");

  useEffect(() => {
    const query = globalQuery.trim();
    if (!query) {
      setSearchResults([]);
      return undefined;
    }
    let active = true;
    const timer = setTimeout(async () => {
      try {
        const [productResults, customerResults, invoiceResults] = await Promise.all([products.list(query), customers.list(query), sales.list()]);
        if (!active) return;
        const invoiceMatches = invoiceResults.filter(item => `${item.invoiceNo || ""} ${item.customerName || ""} ${item.customerPhone || ""}`.toLowerCase().includes(query.toLowerCase())).slice(0, 5);
        setSearchResults([
          ...productResults.slice(0, 5).map(item => ({ type: "Product", label: item.productName, detail: item.productCode, path: "/products" })),
          ...customerResults.slice(0, 5).map(item => ({ type: "Customer", label: item.customerName, detail: item.phone || item.customerCode, path: "/customers" })),
          ...invoiceMatches.map(item => ({ type: "Invoice", label: item.invoiceNo, detail: item.customerName || "Walk-in Customer", path: `/sales?invoiceId=${item.id}` }))
        ]);
      } catch {
        if (active) setSearchResults([]);
      }
    }, 250);
    return () => {
      active = false;
      clearTimeout(timer);
    };
  }, [globalQuery]);

  return <div className={`shell ${mini ? "mini" : ""}`}>
    <aside>
      <div className="brand"><b>D</b><div><strong>DBM POS</strong><small>Commercial Edition</small></div></div>
      <div className="menus">{menu.map((item, index) => <Item key={index} item={item} openPath={openPath} setOpenPath={setOpenPath} />)}</div>
    </aside>
    <section className="main">
      <header>
        <button className="hamb" onClick={() => setMini(value => !value)}>☰</button>
        <button className="pos-launch" title="Open full-screen POS" onClick={() => navigate("/pos")}>POS</button>
        <div className="searchtop" style={{ position: "relative", width: "410px" }}><span>⌕</span><input aria-label="Global search" value={globalQuery} onChange={event => setGlobalQuery(event.target.value)} placeholder="Search products, invoices, customers..." style={{ border: 0, outline: 0, background: "transparent", width: "100%", height: "100%", padding: 0 }} />{searchResults.length > 0 && <div style={{ position: "absolute", top: "42px", left: 0, right: 0, zIndex: 20, background: "#fff", border: "1px solid #e6eaf0", borderRadius: "8px", boxShadow: "0 10px 24px rgba(23,32,51,.12)", overflow: "hidden" }}>{searchResults.map((result, index) => <button key={`${result.type}-${result.label}-${index}`} onMouseDown={() => { setGlobalQuery(""); navigate(result.path); }} style={{ display: "flex", justifyContent: "space-between", width: "100%", padding: "10px 12px", border: 0, borderBottom: "1px solid #eef1f5", background: "#fff", textAlign: "left" }}><span><b>{result.label}</b><small style={{ display: "block", color: "#8793a4" }}>{result.detail}</small></span><small style={{ color: "#2b64df" }}>{result.type}</small></button>)}</div>}</div>
        <div className="user"><div className="avatar">{(user.fullName || "A")[0]}</div><div><b>{user.fullName || "Administrator"}</b><small>{user.roles?.[0] || "User"}</small></div><button onClick={() => { localStorage.clear(); navigate("/login"); }}>Logout</button></div>
      </header>
      <main>{children}</main>
    </section>
  </div>;
}
