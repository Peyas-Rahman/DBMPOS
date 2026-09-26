import jsPDF from "jspdf";
import { autoTable } from "jspdf-autotable";

const escapeHtml = value => String(value ?? "").replace(/[&<>"']/g, char => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;" }[char]));
const amount = value => Number(value || 0).toLocaleString("en-BD", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const invoiceFileName = value => `${String(value || "Invoice").replace(/[<>:"/\\|?*]/g, "-")}.pdf`;

export function downloadPaymentReceipt(payment, customer) {
  const receiptNo = payment.referenceNo || `PAY-${String(payment.id || Date.now()).slice(0, 12)}`;
  const paid = Number(payment.credit || 0);
  const currentBalance = Number(payment.balance || 0);
  const previousBalance = currentBalance + paid;
  const pdf = new jsPDF();
  pdf.setFontSize(19);
  pdf.text("CUSTOMER PAYMENT RECEIPT", 14, 20);
  pdf.setFontSize(10);
  pdf.text(`Receipt No: ${receiptNo}`, 14, 30);
  pdf.text(`Date: ${new Date(payment.transactionDate).toLocaleString("en-BD")}`, 14, 36);
  pdf.text(`Customer: ${customer.customerName || "Customer"}`, 14, 48);
  pdf.text(`Customer Code: ${customer.customerCode || "-"}`, 14, 54);
  if (customer.phone) pdf.text(`Phone: ${customer.phone}`, 14, 60);
  autoTable(pdf, {
    startY: 70,
    body: [
      ["Payment method", payment.description?.replace(/^Payment via /, "") || "Cash"],
      ["Previous due", amount(previousBalance)],
      ["Paid amount", amount(paid)],
      ["Current due", amount(currentBalance)]
    ],
    theme: "grid",
    styles: { fontSize: 11 },
    columnStyles: { 0: { fontStyle: "bold", cellWidth: 70 }, 1: { halign: "right" } }
  });
  pdf.setFontSize(10);
  pdf.text(currentBalance > 0 ? `Remaining due: ${amount(currentBalance)}` : "Due cleared in full", 14, (pdf.lastAutoTable?.finalY || 70) + 16);
  pdf.save(invoiceFileName(receiptNo));
}

export function downloadSalesReturnReceipt(invoice, result, items) {
  const receiptNo = `RET-${String(invoice.invoiceNo || invoice.InvoiceNo || Date.now()).replace(/[^a-zA-Z0-9-]/g, "-")}-${Date.now().toString().slice(-5)}`;
  const pdf = new jsPDF();
  pdf.setFontSize(18);
  pdf.text("SALES RETURN RECEIPT", 14, 20);
  pdf.setFontSize(10);
  pdf.text(`Receipt No: ${receiptNo}`, 14, 30);
  pdf.text(`Original invoice: ${invoice.invoiceNo || invoice.InvoiceNo}`, 14, 36);
  pdf.text(`Customer: ${invoice.customer?.customerName || "Walk-in Customer"}`, 14, 42);
  autoTable(pdf, { startY: 50, head: [["Product", "Returned qty"]], body: items.map(item => [item.name, item.quantity]), styles: { fontSize: 9 }, headStyles: { fillColor: [23, 32, 51] } });
  const y = (pdf.lastAutoTable?.finalY || 50) + 12;
  pdf.text(`Return total: ${amount(result.returnTotal)}`, 14, y);
  pdf.text(`Customer credit: ${amount(result.customerCredit)}`, 14, y + 7);
  pdf.text(`Current due: ${amount(result.currentDue)}`, 14, y + 14);
  pdf.save(invoiceFileName(receiptNo));
}

export function downloadInvoicePdf(sale, { customer, soldBy, companyName = "DBM POS", branchName = "", warehouseName = "" } = {}) {
  const lines = sale.items || sale.Items || [];
  const invoiceNo = sale.invoiceNo || sale.InvoiceNo || "Invoice";
  const pdf = new jsPDF();
  const customerName = customer?.customerName || customer?.CustomerName || "Walk-in Customer";
  const sellerName = soldBy || sale.soldBy || sale.SoldBy || sale.user?.fullName || sale.User?.FullName || "-";
  pdf.setFontSize(18);
  pdf.text(companyName, 14, 18);
  pdf.setFontSize(13);
  pdf.text("SALES INVOICE", 196, 16, { align: "right" });
  pdf.setFontSize(10);
  pdf.text(`Invoice No: ${invoiceNo}`, 196, 23, { align: "right" });
  pdf.text(`${branchName}${warehouseName ? ` · ${warehouseName}` : ""}`, 14, 25);
  pdf.text(`Sold by: ${sellerName}`, 14, 31);
  pdf.text(`Customer: ${customerName}`, 14, 42);
  if (customer?.phone || customer?.Phone) pdf.text(`Phone: ${customer.phone || customer.Phone}`, 14, 48);
  autoTable(pdf, {
    startY: 56,
    head: [["Item", "Qty", "Unit price", "Discount", "Total"]],
    body: lines.map(item => {
      const productName = item.product?.productName || item.Product?.ProductName || item.name || item.productName || "Product";
      const variantName = item.variant?.variantName || item.Variant?.VariantName;
      const quantity = item.quantity ?? item.Quantity ?? 0;
      const unitPrice = item.unitPrice ?? item.UnitPrice ?? item.price ?? 0;
      const discount = item.discount ?? item.Discount ?? 0;
      const lineTotal = item.lineTotal ?? item.LineTotal ?? Number(quantity) * Number(unitPrice) - Number(discount);
      return [`${productName}${variantName ? ` / ${variantName}` : ""}`, quantity, amount(unitPrice), amount(discount), amount(lineTotal)];
    }),
    styles: { fontSize: 9 },
    headStyles: { fillColor: [23, 32, 51] }
  });
  const totals = [
    ["Subtotal", amount(sale.subTotal ?? sale.SubTotal)],
    ["Discount", amount(sale.discount ?? sale.Discount)],
    ["Tax", amount(sale.tax ?? sale.Tax)],
    ["Grand total", amount(sale.grandTotal ?? sale.GrandTotal)],
    ["Paid", amount(sale.paidAmount ?? sale.PaidAmount)],
    ["Due", amount(sale.dueAmount ?? sale.DueAmount)]
  ];
  const startY = (pdf.lastAutoTable?.finalY || 50) + 10;
  pdf.setFontSize(10);
  totals.forEach(([label, value], index) => pdf.text(`${label}: ${value}`, 196, startY + index * 6, { align: "right" }));
  pdf.save(invoiceFileName(invoiceNo));
}

export function printInvoice(sale, { customer, soldBy, companyName = "DBM POS", branchName = "", warehouseName = "" } = {}) {
  const lines = sale.items || sale.Items || [];
  const invoiceNo = sale.invoiceNo || sale.InvoiceNo || "Invoice";
  const previousTitle = document.title;
  const customerName = customer?.customerName || customer?.CustomerName || "Walk-in Customer";
  const sellerName = soldBy || sale.soldBy || sale.SoldBy || sale.user?.fullName || sale.User?.FullName || "-";
  const html = `<!doctype html><html><head><meta charset="utf-8"><title>${escapeHtml(invoiceNo)}</title><style>
    *{box-sizing:border-box}body{font:14px Arial,sans-serif;color:#172033;margin:32px auto;max-width:760px;padding:0 28px}header{display:flex;justify-content:space-between;border-bottom:2px solid #172033;padding-bottom:18px}h1{font-size:24px;margin:0 0 6px}h2{font-size:17px;margin:24px 0 8px}.muted{color:#687386}.meta{text-align:right}.customer{margin:20px 0;padding:14px;background:#f4f6f8}.customer p{margin:4px 0}table{width:100%;border-collapse:collapse;margin:18px 0}th,td{text-align:left;padding:10px 8px;border-bottom:1px solid #dfe3e8}th:last-child,td:last-child{text-align:right}.totals{margin-left:auto;width:280px}.totals div{display:flex;justify-content:space-between;padding:5px 0}.grand{font-size:18px;font-weight:bold;border-top:2px solid #172033;margin-top:6px;padding-top:10px!important}.footer{margin-top:32px;text-align:center;color:#687386}@media print{body{margin:0 auto;padding:12mm}.customer{background:#f4f6f8!important;print-color-adjust:exact}}
    </style></head><body><header><div><h1>${escapeHtml(companyName)}</h1><div class="muted">${escapeHtml(branchName)}${warehouseName ? ` · ${escapeHtml(warehouseName)}` : ""}</div><div class="muted">Sold by: ${escapeHtml(sellerName)}</div></div><div class="meta"><h2>SALES INVOICE</h2><div>Invoice No: <strong>${escapeHtml(invoiceNo)}</strong></div><div class="muted">${escapeHtml(new Date(sale.saleDate || sale.SaleDate || Date.now()).toLocaleString("en-BD"))}</div></div></header>
    <section class="customer"><strong>Customer</strong><p>${escapeHtml(customerName)}</p>${customer?.phone || customer?.Phone ? `<p>${escapeHtml(customer.phone || customer.Phone)}</p>` : ""}${customer?.email || customer?.Email ? `<p>${escapeHtml(customer.email || customer.Email)}</p>` : ""}${customer?.address || customer?.Address ? `<p>${escapeHtml(customer.address || customer.Address)}</p>` : ""}</section>
    <table><thead><tr><th>Item</th><th>Qty</th><th>Unit price</th><th>Discount</th><th>Total</th></tr></thead><tbody>${lines.map(item => {
      const productName = item.product?.productName || item.Product?.ProductName || item.name || item.productName || "Product";
      const variantName = item.variant?.variantName || item.Variant?.VariantName;
      const quantity = item.quantity ?? item.Quantity ?? 0;
      const unitPrice = item.unitPrice ?? item.UnitPrice ?? item.price ?? 0;
      const discount = item.discount ?? item.Discount ?? 0;
      const lineTotal = item.lineTotal ?? item.LineTotal ?? (Number(quantity) * Number(unitPrice) - Number(discount));
      return `<tr><td>${escapeHtml(productName)}${variantName ? `<div class="muted">${escapeHtml(variantName)}</div>` : ""}</td><td>${escapeHtml(quantity)}</td><td>${amount(unitPrice)}</td><td>${amount(discount)}</td><td>${amount(lineTotal)}</td></tr>`;
    }).join("")}</tbody></table><section class="totals"><div><span>Subtotal</span><span>${amount(sale.subTotal ?? sale.SubTotal)}</span></div><div><span>Discount</span><span>${amount(sale.discount ?? sale.Discount)}</span></div><div><span>Tax</span><span>${amount(sale.tax ?? sale.Tax)}</span></div><div class="grand"><span>Grand total</span><span>${amount(sale.grandTotal ?? sale.GrandTotal)}</span></div><div><span>Paid</span><span>${amount(sale.paidAmount ?? sale.PaidAmount)}</span></div><div><span>Due</span><span>${amount(sale.dueAmount ?? sale.DueAmount)}</span></div>${Number(sale.changeAmount || 0) > 0 ? `<div><span>Change returned</span><span>${amount(sale.changeAmount)}</span></div>` : ""}</section><div class="footer">Thank you for your business.</div></body></html>`;
  const frame = document.createElement("iframe");
  frame.title = invoiceNo;
  frame.style.cssText = "position:fixed;width:0;height:0;border:0;right:0;bottom:0";
  frame.onload = () => {
    frame.contentDocument.title = invoiceNo;
    frame.contentWindow?.focus();
    document.title = invoiceNo;
    frame.contentWindow?.print();
    window.setTimeout(() => {
      document.title = previousTitle;
      frame.remove();
    }, 1000);
  };
  frame.srcdoc = html;
  document.body.appendChild(frame);
}
