(function (window) {
    const details = window.PrDetails;

    details.renderGenerateForm = function ({ procurementId, documentTypeId, prId }) {
        return `<div><form action="/PurchaseRequisitions/GenerateDocument" method="post" class="d-inline js-generate-form" data-generate-form="true">
            <input type="hidden" name="__RequestVerificationToken" value="${details.token()}" />
            <input type="hidden" name="prId" value="${details.escapeAttr(prId || details.prId())}" />
            <input type="hidden" name="procurementId" value="${details.escapeAttr(procurementId)}" />
            <input type="hidden" name="documentTypeId" value="${details.escapeAttr(documentTypeId)}" />
            <button type="submit" class="btn btn-sm btn-success js-generate-btn"><i class="bi bi-file-earmark-arrow-down me-1"></i> Generate</button>
        </form></div>`;
    };

    details.renderUploadForm = function ({ rowIndex, procurementId, documentTypeId }) {
        return `<form class="d-inline-flex gap-2 align-items-center flex-nowrap upload-form" action="/ProcurementDocuments/Upload" method="post" enctype="multipart/form-data" hx-boost="false" data-row="${rowIndex}" data-upload-form="true">
            <input type="hidden" name="__RequestVerificationToken" value="${details.token()}" />
            <input type="hidden" name="ProcurementId" value="${details.escapeAttr(procurementId)}" />
            <input type="hidden" name="DocumentTypeId" value="${details.escapeAttr(documentTypeId)}" />
            <label class="upload-dropzone" data-row="${rowIndex}" tabindex="0">
                <span class="fw-semibold small"><i class="bi bi-cloud-arrow-up me-1"></i>Drop/Select (PDF)</span>
                <input class="d-none file-input" type="file" name="File" accept="application/pdf,.pdf" required data-row="${rowIndex}" />
            </label>
            <div class="selected-file small text-muted text-nowrap" data-row="${rowIndex}"><i class="bi bi-info-circle me-1"></i>No file</div>
            <button class="btn btn-sm btn-primary upload-btn" type="submit" disabled data-row="${rowIndex}"><i class="bi bi-upload"></i> Upload</button>
            <div class="upload-feedback small text-muted" data-row="${rowIndex}"></div>
        </form>`;
    };

    details.renderActionButtons = function ({ documentId, documentName, fileName, procurementId, documentTypeId, isGenerated, prId }) {
        const safeDoc = details.escapeAttr(documentId);
        const safeFile = details.escapeAttr(fileName);
        const safeName = details.escapeAttr(documentName || fileName);

        return `<div class="btn-group btn-group-sm" role="group">
            <button type="button" class="btn btn-outline-secondary btn-preview" data-doc-id="${safeDoc}" data-doc-name="${safeName}" data-file-name="${safeFile}" title="Preview"><i class="bi bi-eye"></i></button>
            <button type="button" class="btn btn-outline-primary btn-download" data-doc-id="${safeDoc}" data-file-name="${safeFile}" title="Download"><i class="bi bi-download"></i></button>
            <button type="button" class="btn btn-outline-danger btn-delete" data-doc-id="${safeDoc}" data-doc-name="${safeName}" data-file-name="${safeFile}" data-procurement-id="${details.escapeAttr(procurementId)}" data-document-type-id="${details.escapeAttr(documentTypeId)}" data-is-generated="${isGenerated ? 'true' : 'false'}" data-pr-id="${details.escapeAttr(prId || '')}" title="Delete"><i class="bi bi-trash"></i></button>
        </div>`;
    };

    details.ensureActionsWrapper = function (actionsCell) {
        let wrapper = actionsCell.querySelector('.d-flex.gap-2');
        if (!wrapper) {
            actionsCell.insertAdjacentHTML('afterbegin', '<div class="d-flex gap-2 justify-content-end flex-wrap"></div>');
            wrapper = actionsCell.querySelector('.d-flex.gap-2');
        }
        return wrapper;
    };
})(window);
