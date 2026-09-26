export const money = value =>
  `৳${Number(value || 0).toLocaleString("en-BD", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  })}`;

export const dateTime = value =>
  value ? new Date(value).toLocaleString("en-BD") : "-";
