"use strict";

const DATA_ROOT = "TeaMobi/DuLieu";
const PAGE_SIZE = 36;

const CLASS_NAMES = ["Chiến binh", "Sát thủ", "Pháp sư", "Xạ thủ"];
const FIELD_LABELS = {
  id: "ID", iconId: "ID ảnh", ten: "Tên", moTa: "Mô tả", gia: "Giá",
  loai: "Loại", loaiTien: "Loại tiền", coTheBan: "Có thể bán", giaTri: "Giá trị",
  coTheGiaoDich: "Có thể giao dịch", mauTen: "Màu tên", part: "Part",
  lopNhanVat: "Lớp nhân vật", chiSo: "Chỉ số", cap: "Cấp", hp: "HP",
  kieuDiChuyen: "Kiểu di chuyển", tamDanh: "Tầm đánh", loaiBuff: "Loại buff",
  hieuUngPhu: "Hiệu ứng phụ", cacCap: "Các cấp", thoiGianThiTrien: "Thời gian thi triển",
  kieuVe: "Kiểu vẽ", dau: "Đầu", toc: "Tóc", mat: "Mắt", dangMac: "Đang mặc",
  clan: "Clan", idMini: "ID mini", xNhanVat: "X nhân vật", yNhanVat: "Y nhân vật",
  rong: "Rộng", cao: "Cao", boTile: "Bộ tile", tile: "Tile", loiRa: "Lối ra",
  khu: "Khu", loiThoai: "Lời thoại", anh: "Ảnh", x: "X", y: "Y",
  soKhung: "Số khung", avatar: "Avatar", trangThai: "Trạng thái", chinh: "Nhiệm vụ chính",
  loaiMucTieu: "Loại mục tiêu", npcGiao: "NPC giao", npcTra: "NPC trả", tomTat: "Tóm tắt",
  huongDan: "Hướng dẫn", mucTieu: "Mục tiêu", mau: "Màu", laPhanTram: "Phần trăm",
  dx: "Dịch X", dy: "Dịch Y", diem: "Điểm neo"
};

const CATEGORIES = {
  equipment: {
    code: "TB", title: "Trang bị", short: "Trang bị", file: "EquipmentItems.json", layout: "cards",
    description: "Mẫu trang bị, lớp sử dụng, vị trí và các chỉ số cơ bản.",
    icon: (x) => numberOrNull(x.iconId) === null ? null : Number(x.iconId) + 2000,
    detail: (x) => x.chiSo?.length ? `${x.chiSo.length} chỉ số` : "Chưa có chỉ số",
    tags: (x) => [className(x.lopNhanVat), `loại ${x.loai}`, `part ${x.part}`],
    metric: (x) => x.chiSo?.length ? `${x.chiSo.length} CS` : ""
  },
  potions: {
    code: "VP", title: "Vật phẩm", short: "Vật phẩm", file: "Potions.json", layout: "cards",
    description: "Potion và vật phẩm tiêu hao, giá, loại tiền và khả năng giao dịch.",
    icon: (x) => numberOrNull(x.iconId) === null ? null : Number(x.iconId) + 4000,
    detail: (x) => x.moTa || `Giá trị ${formatNumber(x.giaTri)}`,
    tags: (x) => [`loại ${x.loai}`, tradeLabel(x.coTheGiaoDich)],
    metric: (x) => x.gia !== undefined ? formatNumber(x.gia) : ""
  },
  materials: {
    code: "NL", title: "Nguyên liệu", short: "Nguyên liệu", file: "Materials.json", layout: "cards",
    description: "Nguyên liệu chế tạo và nâng cấp nhận được từ bảng vật phẩm máy chủ.",
    icon: (x) => numberOrNull(x.iconId) === null ? null : Number(x.iconId) + 5500,
    detail: (x) => x.moTa || `Giá trị ${formatNumber(x.giaTri)}`,
    tags: (x) => [`loại ${x.loai}`, tradeLabel(x.coTheGiaoDich)],
    metric: (x) => x.gia !== undefined ? formatNumber(x.gia) : ""
  },
  monsters: {
    code: "Q", title: "Quái", short: "Quái", file: "Monsters.json", layout: "cards",
    description: "Danh mục quái gồm cấp độ, HP và kiểu di chuyển.",
    icon: (x) => numberOrNull(x.id) === null ? null : Number(x.id) + 1000,
    detail: (x) => `Cấp ${formatNumber(x.cap)} · ${formatNumber(x.hp)} HP`,
    tags: (x) => [`cấp ${x.cap}`, `di chuyển ${x.kieuDiChuyen}`],
    metric: (x) => `${formatNumber(x.hp)} HP`
  },
  skills: {
    code: "KN", title: "Kỹ năng", short: "Kỹ năng", file: "Skills.json", layout: "cards",
    description: "Bộ kỹ năng thu được từ các nhân vật đã cấu hình, kèm toàn bộ cấp kỹ năng.",
    icon: (x) => numberOrNull(x.iconId) === null ? null : (Number(x.iconId) & 255) + 6000,
    detail: (x) => x.moTa || `${x.cacCap?.length || 0} cấp kỹ năng`,
    tags: (x) => [`loại ${x.loai}`, `${x.cacCap?.length || 0} cấp`, `tầm ${x.tamDanh}`],
    metric: (x) => x.cacCap?.length ? `${x.cacCap.length} cấp` : ""
  },
  maps: {
    code: "BĐ", title: "Bản đồ", short: "Bản đồ", file: "Maps.json", layout: "cards",
    description: "Các bản đồ nhân vật đã đi qua, kích thước, bộ tile, khu và lối ra.",
    icon: () => null,
    detail: (x) => x.loiRa?.length ? x.loiRa.map((y) => y.ten).filter(Boolean).join(" · ") : "Chưa ghi nhận lối ra",
    tags: (x) => [`${x.rong || 0}×${x.cao || 0}`, `khu ${x.khu}`, `tile ${x.boTile}`],
    metric: (x) => `${x.loiRa?.length || 0} lối ra`
  },
  npcs: {
    code: "NPC", title: "NPC", short: "NPC", file: "Npcs.json", layout: "cards",
    description: "NPC đã gặp trên bản đồ, lời thoại, mô tả và vị trí.",
    icon: (x) => numberOrNull(x.anh) === null ? null : (Number(x.anh) & 255) + 3000,
    detail: (x) => x.moTa || x.loiThoai || `Vị trí ${x.x}, ${x.y}`,
    tags: (x) => [`ảnh ${x.anh}`, `${x.rong || 0}×${x.cao || 0}`, `${x.soKhung || 0} khung`],
    metric: (x) => `x${x.x} y${x.y}`
  },
  quests: {
    code: "NV", title: "Nhiệm vụ", short: "Nhiệm vụ", file: "Quests.json", layout: "cards",
    description: "Nhiệm vụ và tiến độ server gửi cho các nhân vật đã đăng nhập.",
    icon: () => null,
    detail: (x) => x.tomTat || x.huongDan || x.loiThoai || "Không có mô tả",
    tags: (x) => [x.chinh ? "nhiệm vụ chính" : "nhiệm vụ phụ", `trạng thái ${x.trangThai}`],
    metric: (x) => x.mucTieu?.length ? `${x.mucTieu.length} mục tiêu` : ""
  },
  options: {
    code: "CS", title: "Chỉ số", short: "Chỉ số", file: "ItemOptions.json", layout: "table",
    description: "Tên và định dạng các chỉ số/option dùng trong vật phẩm và kỹ năng.",
    icon: () => null,
    detail: (x) => x.laPhanTram ? "Giá trị phần trăm" : "Giá trị số",
    tags: (x) => [`màu ${x.mau}`, x.laPhanTram ? "phần trăm" : "số"],
    metric: () => ""
  },
  characters: {
    code: "NVẬT", title: "Nhân vật", short: "Nhân vật", file: "Characters.json", layout: "cards",
    description: "Danh sách nhân vật ở nguồn đăng nhập gần nhất.",
    icon: () => null,
    detail: (x) => `${className(x.lop)} · cấp ${x.cap}`,
    tags: (x) => [className(x.lop), `cấp ${x.cap}`, x.clan?.tenTat ? `clan ${x.clan.tenTat}` : "không clan"],
    metric: (x) => `cấp ${x.cap}`
  },
  mapitems: {
    code: "TM", title: "Mẫu tile", short: "Mẫu tile", file: "MapItemTemplates.json", layout: "table",
    description: "Mẫu ảnh và các điểm neo dùng để cấu thành bản đồ.",
    icon: (x) => numberOrNull(x.anh),
    detail: (x) => `${x.diem?.length || 0} điểm neo`,
    tags: (x) => [`ảnh ${x.anh}`, `${x.diem?.length || 0} điểm`],
    metric: (x) => `${x.diem?.length || 0} điểm`
  }
};

const state = {
  category: "equipment",
  query: "",
  sort: "id-asc",
  page: 0,
  data: [],
  filtered: [],
  selected: null,
  cache: new Map(),
  request: 0,
  images: null,
  invoker: null,
  hasCategory: false
};

const el = (id) => document.getElementById(id);
const routeContainer = el("routes");
const grid = el("record-grid");
const pagination = el("pagination");
const searchInput = el("search");
const sortSelect = el("sort");

function escapeHtml(value) {
  return String(value ?? "").replace(/[&<>"']/g, (char) => ({
    "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
  })[char]);
}

function normalize(value) {
  return String(value ?? "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/đ/g, "d").replace(/Đ/g, "D").toLowerCase();
}

function numberOrNull(value) {
  return value === null || value === undefined || value === "" || Number.isNaN(Number(value)) ? null : Number(value);
}

function recordId(record) {
  return record.id ?? record.iconId ?? 0;
}

function recordName(record) {
  return record.ten || record.name || `Mục #${recordId(record)}`;
}

function formatNumber(value) {
  const number = Number(value);
  return Number.isFinite(number) ? new Intl.NumberFormat("vi-VN").format(number) : String(value ?? "—");
}

function className(value) {
  const number = Number(value);
  if (number < 0 || !Number.isFinite(number)) return "Dùng chung";
  return CLASS_NAMES[number] || `Lớp ${number}`;
}

function tradeLabel(value) {
  return value === true || Number(value) > 0 ? "giao dịch được" : "không giao dịch";
}

function compactText(value, fallback = "Không có mô tả") {
  const text = String(value ?? "").replace(/\s+/g, " ").trim();
  return text || fallback;
}

function iconPath(id, extension = "png") {
  return `${DATA_ROOT}/Icons/${Math.floor(Number(id) / 1000)}/${id}.${extension}`;
}

function iconMarkup(id, label, large = false) {
  if (id === null || id === undefined || (state.images && !state.images.has(Number(id)))) {
    return `<span class="icon-frame empty${large ? " large" : ""}" aria-hidden="true"></span>`;
  }
  return `<span class="icon-frame${large ? " large" : ""}"><img src="${iconPath(id)}" data-icon-id="${escapeHtml(id)}" data-ext-index="0" alt="${escapeHtml(label)}" loading="lazy"></span>`;
}

function mountImageFallbacks(root = document) {
  const extensions = ["png", "gif", "jpg"];
  root.querySelectorAll("img[data-icon-id]").forEach((image) => {
    image.addEventListener("error", () => {
      const next = Number(image.dataset.extIndex || 0) + 1;
      if (next < extensions.length) {
        image.dataset.extIndex = String(next);
        image.src = iconPath(image.dataset.iconId, extensions[next]);
      } else {
        image.parentElement?.classList.add("empty");
        image.remove();
      }
    });
  });
}

async function fetchJson(file) {
  if (state.cache.has(file)) return state.cache.get(file);
  const response = await fetch(`${DATA_ROOT}/${file}`, { cache: "no-store" });
  if (!response.ok) throw new Error(`HTTP ${response.status}`);
  const data = await response.json();
  if (!Array.isArray(data)) throw new Error("Dữ liệu không phải danh sách");
  state.cache.set(file, data);
  return data;
}

function renderRoutes() {
  routeContainer.innerHTML = Object.entries(CATEGORIES).map(([key, category]) => `
    <button class="route-button" type="button" data-category="${key}"
      aria-pressed="${key === state.category}">${escapeHtml(category.short)}</button>`).join("");
}

// Chỉ cuộn ngang dải danh mục; scrollIntoView sẽ kéo cả trang xuống dưới header dính.
function centerRoute(key) {
  const button = routeContainer.querySelector(`[data-category="${key}"]`);
  if (!button || routeContainer.scrollWidth <= routeContainer.clientWidth) return;
  const offset = button.getBoundingClientRect().left - routeContainer.getBoundingClientRect().left + routeContainer.scrollLeft;
  const left = offset - (routeContainer.clientWidth - button.offsetWidth) / 2;
  routeContainer.scrollTo({ left: Math.max(0, left), behavior: "auto" });
}

function loadingState() {
  grid.setAttribute("aria-busy", "true");
  grid.innerHTML = `<div class="loading-row" aria-label="Đang tải dữ liệu"><i></i><i></i><i></i><i></i><i></i><i></i></div>`;
  pagination.innerHTML = "";
}

function errorState(category, error) {
  grid.setAttribute("aria-busy", "false");
  const firstRun = /404/.test(error.message);
  grid.innerHTML = `<div class="empty-state"><div>
    <span class="empty-code">${firstRun ? "CHỜ" : "LỖI"}</span>
    <h2>${firstRun ? "Dữ liệu đang được lấy lần đầu" : "Không đọc được dữ liệu"}</h2>
    <p>${firstRun
      ? `File <strong>${escapeHtml(category.file)}</strong> chưa xuất hiện. Workflow đang đăng nhập lần lượt các nhân vật và sẽ tự đưa dữ liệu lên đây.`
      : `${escapeHtml(error.message)}. Bạn có thể thử lại hoặc kiểm tra lượt chạy GitHub Actions.`}</p>
    <div class="empty-actions">
      <button class="button" type="button" data-retry>Thử lại</button>
      <a class="button secondary" href="https://github.com/duykhongphai/DataHso/actions" target="_blank" rel="noreferrer">Xem Actions</a>
    </div>
  </div></div>`;
  el("record-count").textContent = "0";
  el("result-note").textContent = "Chưa có dữ liệu";
  pagination.innerHTML = "";
}

async function loadCategory(key, updateHash = true) {
  if (!CATEGORIES[key]) key = "equipment";
  const category = CATEGORIES[key];
  state.category = key;
  state.page = 0;
  state.selected = null;
  closeDetail(false);
  renderRoutes();
  el("route-code").textContent = category.code;
  el("route-name").textContent = category.title;
  el("stage-title").textContent = category.title;
  el("stage-description").textContent = category.description;
  if (state.hasCategory) {
    const routeCurrent = document.querySelector(".route-current");
    const stageHeading = document.querySelector(".stage-heading");
    routeCurrent.classList.remove("route-swap");
    stageHeading.classList.remove("route-swap");
    void routeCurrent.offsetWidth;
    routeCurrent.classList.add("route-swap");
    stageHeading.classList.add("route-swap");
  }
  state.hasCategory = true;
  el("raw-link").href = `${DATA_ROOT}/${category.file}`;
  if (updateHash && location.hash !== `#/${key}`) history.pushState(null, "", `#/${key}`);
  loadingState();
  const request = ++state.request;
  try {
    const data = await fetchJson(category.file);
    if (request !== state.request) return;
    state.data = data;
    applyFilters();
    centerRoute(key);
  } catch (error) {
    if (request !== state.request) return;
    state.data = [];
    state.filtered = [];
    errorState(category, error);
  }
}

function applyFilters(resetPage = false) {
  if (resetPage) state.page = 0;
  const query = normalize(state.query.trim());
  state.filtered = state.data.filter((record) => !query || normalize(JSON.stringify(record)).includes(query));
  const [field, direction] = state.sort.split("-");
  state.filtered.sort((a, b) => {
    const left = field === "name" ? normalize(recordName(a)) : Number(recordId(a));
    const right = field === "name" ? normalize(recordName(b)) : Number(recordId(b));
    const compared = typeof left === "string" ? left.localeCompare(right, "vi") : left - right;
    return direction === "desc" ? -compared : compared;
  });
  renderRecords();
}

function recordTags(category, record) {
  return (category.tags(record) || []).filter((tag) => tag !== null && tag !== undefined && tag !== "")
    .slice(0, 4).map((tag) => `<span class="tag">${escapeHtml(tag)}</span>`).join("");
}

function copyIcon() {
  return `<svg aria-hidden="true" viewBox="0 0 24 24"><rect x="8" y="8" width="12" height="12" rx="2"/><path d="M16 8V6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h2"/></svg>`;
}

function renderCard(category, record, absoluteIndex) {
  const id = recordId(record);
  const name = recordName(record);
  const icon = category.icon(record);
  return `<article class="record-card" role="button" tabindex="0" data-index="${absoluteIndex}" aria-selected="false">
    <div class="card-main">
      ${iconMarkup(icon, name)}
      <div class="card-copy">
        <h2 class="card-title">${escapeHtml(name)}</h2>
        <div class="card-description">${escapeHtml(compactText(category.detail(record)))}</div>
        <div class="tags">${recordTags(category, record)}</div>
      </div>
    </div>
    <div class="card-foot">
      <button class="id-chip" type="button" data-copy="${escapeHtml(id)}" title="Chép ID">${copyIcon()} ID ${escapeHtml(id)}</button>
      <span class="card-metric">${escapeHtml(category.metric(record) || "")}</span>
    </div>
  </article>`;
}

function renderTable(category, records, start) {
  const rows = records.map((record, index) => `<tr tabindex="0" data-index="${start + index}">
    <td class="mono"><button class="id-chip" type="button" data-copy="${escapeHtml(recordId(record))}">${copyIcon()} ${escapeHtml(recordId(record))}</button></td>
    <td><strong>${escapeHtml(recordName(record))}</strong></td>
    <td>${escapeHtml(compactText(category.detail(record), "—"))}</td>
    <td class="number">${escapeHtml(category.metric(record) || "")}</td>
  </tr>`).join("");
  return `<div class="table-shell"><table class="data-table">
    <thead><tr><th>ID</th><th>Tên</th><th>Chi tiết</th><th class="number">Giá trị</th></tr></thead>
    <tbody>${rows}</tbody>
  </table></div>`;
}

function renderRecords() {
  const category = CATEGORIES[state.category];
  const total = state.filtered.length;
  const pageCount = Math.max(1, Math.ceil(total / PAGE_SIZE));
  state.page = Math.min(state.page, pageCount - 1);
  const start = state.page * PAGE_SIZE;
  const records = state.filtered.slice(start, start + PAGE_SIZE);
  el("record-count").textContent = formatNumber(total);
  el("result-note").textContent = total ? `Hiển thị ${formatNumber(start + 1)}–${formatNumber(Math.min(start + PAGE_SIZE, total))}` : "0 kết quả";
  grid.setAttribute("aria-busy", "false");

  if (!records.length) {
    grid.innerHTML = `<div class="empty-state"><div><span class="empty-code">0</span>
      <h2>Không tìm thấy mục phù hợp</h2><p>Thử từ khóa ngắn hơn hoặc xóa bộ lọc tìm kiếm.</p>
      <div class="empty-actions"><button class="button" type="button" data-clear-search>Xóa tìm kiếm</button></div>
    </div></div>`;
  } else if (category.layout === "table") {
    grid.innerHTML = renderTable(category, records, start);
  } else {
    grid.innerHTML = `<div class="card-grid">${records.map((record, index) => renderCard(category, record, start + index)).join("")}</div>`;
  }
  mountImageFallbacks(grid);
  renderPagination(pageCount);
}

function renderPagination(pageCount) {
  if (pageCount <= 1) {
    pagination.innerHTML = "";
    return;
  }
  const pages = new Set([0, pageCount - 1, state.page - 1, state.page, state.page + 1]);
  const visible = [...pages].filter((page) => page >= 0 && page < pageCount).sort((a, b) => a - b);
  let previous = -1;
  const middle = visible.map((page) => {
    const gap = previous >= 0 && page - previous > 1 ? `<span class="page-button" aria-hidden="true">…</span>` : "";
    previous = page;
    return `${gap}<button class="page-button" type="button" data-page="${page}" ${page === state.page ? 'aria-current="page"' : ""}>${page + 1}</button>`;
  }).join("");
  pagination.innerHTML = `
    <button class="page-button" type="button" data-page="${state.page - 1}" ${state.page === 0 ? "disabled" : ""}>← Trước</button>
    ${middle}
    <button class="page-button" type="button" data-page="${state.page + 1}" ${state.page === pageCount - 1 ? "disabled" : ""}>Sau →</button>`;
}

function formatValue(value) {
  if (value === null || value === undefined || value === "") return "—";
  if (typeof value === "boolean") return value ? "Có" : "Không";
  if (typeof value === "number") return formatNumber(value);
  if (typeof value === "object") return `<pre>${escapeHtml(JSON.stringify(value, null, 2))}</pre>`;
  return escapeHtml(value);
}

function openDetail(index, invoker = null) {
  const record = state.filtered[index];
  if (!record) return;
  state.selected = record;
  state.invoker = invoker;
  const category = CATEGORIES[state.category];
  const name = recordName(record);
  el("detail-kind").textContent = category.title;
  el("detail-title").textContent = name;
  const image = iconMarkup(category.icon(record), name, true);
  const description = compactText(category.detail(record));
  const fields = Object.entries(record).map(([key, value]) => `
    <div class="field-row"><dt>${escapeHtml(FIELD_LABELS[key] || key)}</dt><dd>${formatValue(value)}</dd></div>`).join("");
  el("detail-body").innerHTML = `
    <div class="detail-hero">${image}<div><span class="id-chip">ID ${escapeHtml(recordId(record))}</span><p>${escapeHtml(description)}</p></div></div>
    <dl class="field-list">${fields}</dl>`;
  mountImageFallbacks(el("detail-body"));
  document.querySelectorAll("[data-index]").forEach((node) => node.setAttribute("aria-selected", String(Number(node.dataset.index) === index)));
  const panel = el("detail-panel");
  panel.inert = false;
  panel.classList.add("open");
  panel.setAttribute("aria-hidden", "false");
  el("detail-close").focus({ preventScroll: true });
}

function closeDetail(restoreFocus = true) {
  const panel = el("detail-panel");
  panel.classList.remove("open");
  panel.setAttribute("aria-hidden", "true");
  panel.inert = true;
  state.selected = null;
  document.querySelectorAll("[data-index]").forEach((node) => node.setAttribute("aria-selected", "false"));
  if (restoreFocus && state.invoker?.isConnected) state.invoker.focus({ preventScroll: true });
  state.invoker = null;
}

let toastTimer;
function showToast(message) {
  const toast = el("toast");
  toast.textContent = message;
  toast.classList.add("show");
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => toast.classList.remove("show"), 1800);
}

async function copyText(text, message = "Đã chép") {
  try {
    await navigator.clipboard.writeText(String(text));
  } catch {
    const textarea = document.createElement("textarea");
    textarea.value = String(text);
    document.body.append(textarea);
    textarea.select();
    document.execCommand("copy");
    textarea.remove();
  }
  showToast(message);
}

async function loadFreshness() {
  const freshness = el("freshness");
  try {
    const response = await fetch(`${DATA_ROOT}/LastUpdated`, { cache: "no-store" });
    if (!response.ok) throw new Error(String(response.status));
    const text = (await response.text()).trim();
    const date = new Date(text);
    freshness.className = "freshness ready";
    freshness.querySelector("span:last-child").textContent = Number.isNaN(date.getTime())
      ? "Dữ liệu đã sẵn sàng"
      : `Cập nhật ${new Intl.DateTimeFormat("vi-VN", { dateStyle: "short", timeStyle: "short", timeZone: "Asia/Ho_Chi_Minh" }).format(date)}`;
  } catch {
    freshness.className = "freshness error";
    freshness.querySelector("span:last-child").textContent = "Đang chờ lượt dữ liệu đầu tiên";
  }
}

async function loadImageIndex() {
  try {
    const response = await fetch(`${DATA_ROOT}/Images.json`, { cache: "no-store" });
    if (response.ok) state.images = new Set((await response.json()).map(Number));
  } catch {
    state.images = null;
  }
}

function categoryFromHash() {
  const match = location.hash.match(/^#\/([a-z]+)/i);
  return match && CATEGORIES[match[1]] ? match[1] : "equipment";
}

routeContainer.addEventListener("click", (event) => {
  const button = event.target.closest("[data-category]");
  if (button) loadCategory(button.dataset.category);
});

searchInput.addEventListener("input", () => {
  state.query = searchInput.value;
  applyFilters(true);
});

sortSelect.addEventListener("change", () => {
  state.sort = sortSelect.value;
  applyFilters(true);
});

grid.addEventListener("click", (event) => {
  const copy = event.target.closest("[data-copy]");
  if (copy) {
    event.stopPropagation();
    copyText(copy.dataset.copy, `Đã chép ID ${copy.dataset.copy}`);
    return;
  }
  if (event.target.closest("[data-retry]")) {
    state.cache.delete(CATEGORIES[state.category].file);
    loadCategory(state.category, false);
    return;
  }
  if (event.target.closest("[data-clear-search]")) {
    searchInput.value = "";
    state.query = "";
    applyFilters(true);
    searchInput.focus();
    return;
  }
  const record = event.target.closest("[data-index]");
  if (record) openDetail(Number(record.dataset.index), record);
});

grid.addEventListener("keydown", (event) => {
  if (event.key !== "Enter" && event.key !== " ") return;
  const record = event.target.closest("[data-index]");
  if (!record || event.target.closest("[data-copy]")) return;
  event.preventDefault();
  openDetail(Number(record.dataset.index), record);
});

pagination.addEventListener("click", (event) => {
  const button = event.target.closest("[data-page]");
  if (!button || button.disabled) return;
  state.page = Number(button.dataset.page);
  renderRecords();
  el("records").scrollIntoView({ block: "start" });
});

el("detail-close").addEventListener("click", closeDetail);
el("copy-json").addEventListener("click", () => {
  if (state.selected) copyText(JSON.stringify(state.selected, null, 2), "Đã chép JSON");
});

document.addEventListener("keydown", (event) => {
  if (event.key === "/" && document.activeElement !== searchInput && !/INPUT|TEXTAREA|SELECT/.test(document.activeElement?.tagName)) {
    event.preventDefault();
    searchInput.focus();
  }
  if (event.key === "Escape" && el("detail-panel").classList.contains("open")) closeDetail();
});

window.addEventListener("hashchange", () => loadCategory(categoryFromHash(), false));

async function init() {
  renderRoutes();
  await Promise.allSettled([loadFreshness(), loadImageIndex()]);
  await loadCategory(categoryFromHash(), false);
}

init();
