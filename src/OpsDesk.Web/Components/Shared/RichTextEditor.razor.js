export function createQuill(quillElement, toolBar, readOnly, placeholder, theme, initialContent) {
    if (!quillElement) return;

    if (quillElement.__quill) {
        if (initialContent !== undefined && initialContent !== null) {
            quillElement.__quill.root.innerHTML = initialContent;
        }
        return;
    }

    const options = {
        modules: {
            toolbar: toolBar || false
        },
        placeholder: placeholder || '',
        readOnly: readOnly || false,
        theme: theme || 'snow'
    };

    const quill = new Quill(quillElement, options);
    quillElement.__quill = quill;

    if (initialContent) {
        quill.root.innerHTML = initialContent;
    }
}

export function getHTML(quillElement) {
    if (!quillElement) return '';
    const editor = quillElement.querySelector('.ql-editor');
    return editor ? editor.innerHTML : (quillElement.__quill ? quillElement.__quill.root.innerHTML : '');
}

export function loadHTMLContent(quillElement, htmlContent) {
    if (!quillElement) return;
    if (quillElement.__quill) {
        quillElement.__quill.root.innerHTML = htmlContent || '';
    }
}

export function dispose(quillElement) {
    if (quillElement && quillElement.__quill) {
        delete quillElement.__quill;
    }
}
