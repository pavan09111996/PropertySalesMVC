// Renders the Admin Analytics page's charts from window.analyticsData (set
// inline by Views/Admin/Analytics.cshtml). Ported from the approved mockup —
// same chart forms/colors, wired to real data instead of samples.
(function () {
    const data = window.analyticsData;
    if (!data) return;

    const tooltip = document.getElementById('tooltip');

    function showTooltip(evt, html) {
        tooltip.innerHTML = html;
        tooltip.classList.add('show');
        moveTooltip(evt);
    }
    function moveTooltip(evt) {
        tooltip.style.left = (evt.clientX + 14) + 'px';
        tooltip.style.top = (evt.clientY - 14) + 'px';
    }
    function hideTooltip() { tooltip.classList.remove('show'); }

    // ---- Stacked bar rows (Top Locations / Top BHK) ----
    function renderBarChart(containerId, tbodyId, rows) {
        const container = document.getElementById(containerId);
        const tbody = document.getElementById(tbodyId);
        if (!container || rows.length === 0) return;

        const maxTotal = Math.max(...rows.map(d => d.buy + d.rent));
        rows.sort((a, b) => (b.buy + b.rent) - (a.buy + a.rent));

        rows.forEach(d => {
            const total = d.buy + d.rent;
            const barLengthPct = maxTotal === 0 ? 0 : (total / maxTotal) * 100;
            const buyShare = total === 0 ? 0 : (d.buy / total) * 100;
            const rentShare = total === 0 ? 0 : (d.rent / total) * 100;

            const row = document.createElement('div');
            row.className = 'bar-row';
            row.innerHTML =
                '<span class="row-label"></span>' +
                '<div class="bar-track"><div class="bar-fill-group"><div class="seg buy"></div><div class="seg rent"></div></div></div>' +
                '<span class="row-total"></span>';

            row.querySelector('.row-label').textContent = d.label;
            row.querySelector('.row-total').textContent = total;

            const group = row.querySelector('.bar-fill-group');
            group.style.width = barLengthPct + '%';

            const buySeg = row.querySelector('.seg.buy');
            buySeg.style.width = buyShare + '%';
            buySeg.dataset.series = 'Buy';
            buySeg.dataset.value = d.buy;
            buySeg.dataset.label = d.label;

            const rentSeg = row.querySelector('.seg.rent');
            rentSeg.style.width = rentShare + '%';
            rentSeg.dataset.series = 'Rent';
            rentSeg.dataset.value = d.rent;
            rentSeg.dataset.label = d.label;

            container.appendChild(row);

            if (tbody) {
                const tr = document.createElement('tr');
                const cells = [d.label, d.buy, d.rent, total];
                cells.forEach(text => {
                    const td = document.createElement('td');
                    td.textContent = text;
                    tr.appendChild(td);
                });
                tbody.appendChild(tr);
            }
        });

        container.querySelectorAll('.seg').forEach(seg => {
            seg.addEventListener('mousemove', e => {
                const series = seg.dataset.series, value = seg.dataset.value, label = seg.dataset.label;
                showTooltip(e, '<span class="t-value">' + value + '</span><span class="t-series">' + series + ' · ' + label + '</span>');
            });
            seg.addEventListener('mouseleave', hideTooltip);
        });
    }

    renderBarChart('locations-chart', 'locations-tbody', data.locations || []);
    renderBarChart('bhk-chart', 'bhk-tbody', data.bhk || []);

    document.querySelectorAll('.table-toggle').forEach(btn => {
        btn.addEventListener('click', () => {
            const key = btn.dataset.toggle;
            const chart = document.getElementById(key + '-chart');
            const table = document.getElementById(key + '-table');
            const showingTable = table.classList.contains('show');
            table.classList.toggle('show', !showingTable);
            chart.classList.toggle('hidden', !showingTable);
            btn.textContent = showingTable ? 'View as table' : 'View as chart';
        });
    });

    // ---- Location x BHK heatmap ----
    function hexToRgba(hex, alpha) {
        const r = parseInt(hex.slice(1, 3), 16), g = parseInt(hex.slice(3, 5), 16), b = parseInt(hex.slice(5, 7), 16);
        return 'rgba(' + r + ',' + g + ',' + b + ',' + alpha + ')';
    }

    function renderHeatmap(tableId, hueHex, seriesKey, rows, colLabels, maxVal) {
        const table = document.getElementById(tableId);
        if (!table || rows.length === 0) return;

        let thead = '<thead><tr><th></th>';
        colLabels.forEach(c => { thead += '<th></th>'; });
        thead += '</tr></thead>';
        table.innerHTML = thead + '<tbody></tbody>';

        const headRow = table.querySelector('thead tr');
        colLabels.forEach((c, i) => { headRow.children[i + 1].textContent = c; });

        const tbody = table.querySelector('tbody');
        rows.forEach(row => {
            const tr = document.createElement('tr');
            const th = document.createElement('th');
            th.className = 'row-head';
            th.textContent = row.locationName;
            tr.appendChild(th);

            const values = row[seriesKey];
            values.forEach((val, ci) => {
                const t = maxVal === 0 ? 0 : val / maxVal;
                const opacity = val === 0 ? 0.05 : 0.15 + t * 0.75;
                const textColor = t > 0.55 ? '#FBF9F4' : '#14181C';
                const td = document.createElement('td');
                td.textContent = val;
                td.style.background = hexToRgba(hueHex, opacity);
                td.style.color = textColor;
                if (val === 0) td.classList.add('zero');
                td.dataset.row = row.locationName;
                td.dataset.col = colLabels[ci];
                td.dataset.val = val;
                td.addEventListener('mousemove', e => {
                    showTooltip(e, '<span class="t-value">' + td.dataset.val + '</span><span class="t-series">' + td.dataset.row + ' · ' + td.dataset.col + '</span>');
                });
                td.addEventListener('mouseleave', hideTooltip);
                tr.appendChild(td);
            });

            tbody.appendChild(tr);
        });
    }

    if (data.heatmap && data.heatmap.rows && data.heatmap.rows.length > 0) {
        renderHeatmap('heatmap-buy', '#B8862E', 'buyValues', data.heatmap.rows, data.heatmap.bhkColumns, data.heatmap.maxBuyValue);
        renderHeatmap('heatmap-rent', '#00795F', 'rentValues', data.heatmap.rows, data.heatmap.bhkColumns, data.heatmap.maxRentValue);
    }

    // ---- Daily trend (single series -> 1 hue, no legend needed) ----
    (function renderTrend() {
        const days = data.trend || [];
        const svg = document.getElementById('trend-svg');
        if (!svg || days.length === 0) return;

        const values = days.map(d => d.total);
        const W = 640, H = 180, padL = 30, padR = 10, padT = 12, padB = 24;
        const plotW = W - padL - padR, plotH = H - padT - padB;
        const maxVal = Math.max(5, Math.ceil(Math.max(...values) / 5) * 5);

        const x = i => days.length <= 1 ? padL : padL + (i / (days.length - 1)) * plotW;
        const y = v => padT + plotH - (v / maxVal) * plotH;

        let svgHtml = '';

        const ticks = [0, maxVal / 2, maxVal];
        ticks.forEach(t => {
            svgHtml += '<line class="grid-line" x1="' + padL + '" x2="' + (W - padR) + '" y1="' + y(t) + '" y2="' + y(t) + '" />';
            svgHtml += '<text class="axis-tick" x="2" y="' + (y(t) + 3) + '">' + Math.round(t) + '</text>';
        });

        let areaPath = 'M ' + x(0) + ' ' + y(0) + ' ';
        values.forEach((v, i) => areaPath += 'L ' + x(i) + ' ' + y(v) + ' ');
        areaPath += 'L ' + x(values.length - 1) + ' ' + y(0) + ' Z';
        svgHtml += '<path d="' + areaPath + '" fill="var(--rent)" opacity="0.1" stroke="none" />';

        let linePath = 'M ' + x(0) + ' ' + y(values[0]) + ' ';
        values.forEach((v, i) => { if (i > 0) linePath += 'L ' + x(i) + ' ' + y(v) + ' '; });
        svgHtml += '<path d="' + linePath + '" fill="none" stroke="var(--rent)" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" />';

        const lastI = values.length - 1;
        svgHtml += '<circle class="trend-dot" cx="' + x(lastI) + '" cy="' + y(values[lastI]) + '" r="4" />';
        svgHtml += '<text class="trend-endlabel" x="' + (x(lastI) - 14) + '" y="' + (y(values[lastI]) - 10) + '">' + values[lastI] + '</text>';

        svgHtml += '<line class="trend-crosshair" id="crosshair" x1="0" x2="0" y1="' + padT + '" y2="' + (H - padB) + '" style="display:none;" />';
        values.forEach((v, i) => {
            svgHtml += '<circle cx="' + x(i) + '" cy="' + y(v) + '" r="12" fill="transparent" class="hit" data-i="' + i + '" data-v="' + v + '" data-date="' + days[i].date + '" />';
        });

        svg.innerHTML = svgHtml;

        const crosshair = document.getElementById('crosshair');
        svg.querySelectorAll('.hit').forEach(hit => {
            hit.addEventListener('mousemove', e => {
                crosshair.style.display = 'block';
                crosshair.setAttribute('x1', hit.getAttribute('cx'));
                crosshair.setAttribute('x2', hit.getAttribute('cx'));
                showTooltip(e, '<span class="t-value">' + hit.dataset.v + '</span><span class="t-series">' + hit.dataset.date + '</span>');
            });
            hit.addEventListener('mouseleave', () => {
                crosshair.style.display = 'none';
                hideTooltip();
            });
        });
    })();
})();
