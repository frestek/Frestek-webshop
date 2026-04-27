<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="View.ascx.cs" Inherits="Frestek.Vision.Modul.View" %>

<div class="frestek-vision-container">
    <div class="frestek-ui-card">
        <h2 class="frestek-brand-title">Frestek AI Színválasztó</h2>
        <p class="frestek-instruction">Töltsön fel egy fotót az elemzéshez!</p>

        <div class="frestek-upload-zone">
            <label for="frestek-file-input" class="btn-primary-frestek">
                <i class="fa fa-camera"></i> Kép kiválasztása
            </label>
            <input type="file" id="frestek-file-input" accept="image/*" style="display:none;" />
        </div>

        <div id="frestek-preview-area" style="display:none; margin-top:20px;">
            <canvas id="frestek-analysis-canvas" style="max-width:100%; border-radius:8px;"></canvas>
        </div>

        <div id="frestek-recommendations" style="display:none; margin-top:30px;">
            <h3>Ajánlott festékeink:</h3>
            <div class="product-grid" id="product-grid"></div>
        </div>
    </div>
</div>

<script src="https://cdnjs.cloudflare.com/ajax/libs/color-thief/2.3.0/color-thief.umd.js"></script>

<script type="text/javascript">
    var frestekModuleId = <%=ModuleId%>;
</script>
<script src="<%=ControlPath%>js/vision-logic.js"></script>