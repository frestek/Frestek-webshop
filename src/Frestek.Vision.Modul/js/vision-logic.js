document.addEventListener("DOMContentLoaded", function () {
    const fileInput = document.getElementById('frestek-file-input') || document.querySelector('input[id$="frestek-file-input"]');
    const canvas = document.getElementById('frestek-analysis-canvas') || document.querySelector('canvas[id$="frestek-analysis-canvas"]');
    const productGrid = document.getElementById('product-grid');
    const previewArea = document.getElementById('frestek-preview-area');
    
    let isFetching = false;

    if (!fileInput || !canvas) return;

    fileInput.addEventListener('change', function (e) {
        if (!e.target.files[0]) return;
        
        const reader = new FileReader();
        reader.onload = function (event) {
            const img = new Image();
            img.onload = function () {
                canvas.width = 300;
                canvas.height = (img.height / img.width) * 300;
                const ctx = canvas.getContext('2d');
                ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
                previewArea.style.display = 'block';
                
                // ColorThief használata a domináns színek kinyeréséhez
                const colorThief = new ColorThief();
                // A getPalette(img, 3) visszaadja a 3 legmarkánsabb színt RGB tömbökben
                const palette = colorThief.getPalette(img, 3);
                const hexColors = palette.map(p => rgbToHex(p[0], p[1], p[2]));
                
                renderColorChoices(hexColors);
            };
            img.src = event.target.result;
        };
        reader.readAsDataURL(e.target.files[0]);
    });

    function rgbToHex(r, g, b) {
        return "#" + (1 << 24 | r << 16 | g << 8 | b).toString(16).slice(1).toUpperCase();
    }

    function renderColorChoices(hexColors) {
        let optionsDiv = document.getElementById('frestek-color-options');
        if (!optionsDiv) {
            optionsDiv = document.createElement('div');
            optionsDiv.id = 'frestek-color-options';
            optionsDiv.innerHTML = '<p style="margin-top:15px; font-weight:bold;">Válassza ki a keresett színt a képről:</p><div id="color-picker-grid" style="display:flex; justify-content:center; gap:15px;"></div>';
            previewArea.appendChild(optionsDiv);
        }
        
        const grid = document.getElementById('color-picker-grid');
        grid.innerHTML = '';
        productGrid.innerHTML = '';
        document.getElementById('frestek-recommendations').style.display = 'none';

        hexColors.forEach(hex => {
            const circle = document.createElement('div');
            circle.style.cssText = `width:50px; height:50px; border-radius:50%; background-color:${hex}; cursor:pointer; border:3px solid #ccc; box-shadow: 0 2px 5px rgba(0,0,0,0.2); transition: transform 0.2s;`;
            
            circle.onclick = function() {
                if (isFetching) return;
                Array.from(grid.children).forEach(c => c.style.border = '3px solid #ccc');
                circle.style.border = '3px solid #114B5F';
                fetchProductsByColor(hex);
            };
            grid.appendChild(circle);
        });
    }

    async function fetchProductsByColor(hex) {
        isFetching = true;
        const encodedHex = encodeURIComponent(hex);
        const apiUrl = `/DesktopModules/FrestekVision/API/Vision/GetMatchesByColor?hex=${encodedHex}`;

        try {
            const response = await fetch(apiUrl, {
                method: 'GET',
                headers: { 'ModuleId': typeof frestekModuleId !== 'undefined' ? frestekModuleId : 0 }
            });
            const products = await response.json();
            renderProducts(products);
        } catch (err) {
            console.error("Hiba:", err);
        } finally {
            isFetching = false;
        }
    }

    function renderProducts(products) {
        productGrid.innerHTML = '';
        products.forEach(p => {
            const card = `
                <div class="product-card">
                    <img src="${p.ImageUrl}" alt="${p.ProductName}" style="width:100%; height:auto; border-radius:4px; margin-bottom:10px;">
                    <div style="display:flex; align-items:center; justify-content:center; gap:10px; margin-bottom:10px;">
                        <div style="background:${p.HexCode}; width:20px; height:20px; border-radius:50%; border:1px solid #ccc;"></div>
                        <strong>${p.ProductName}</strong>
                    </div>
                    <small>Cikkszám: ${p.ProductSKU}</small>
                    <p style="color:#114B5F; font-weight:bold; font-size:1.1em; margin: 10px 0;">${p.PriceHUF.toLocaleString()} Ft</p>
                    <a href="${p.ProductUrl}" class="btn-primary-frestek" style="display:block; text-decoration:none; font-size:0.9em;">
                        Vásárlás
                    </a>
                </div>`;
            productGrid.insertAdjacentHTML('beforeend', card);
        });
        document.getElementById('frestek-recommendations').style.display = 'block';
    }
});