// Attendre que SwaggerUI soit chargé
window.addEventListener('load', function() {
    // Attendre que les éléments soient rendus
    setTimeout(function() {
        // Ajouter les styles
        const style = document.createElement('style');
        style.textContent = `
            .opblock-tag-section {
                margin-bottom: 10px;
            }
            .opblock-tag {
                border: 1px solid var(--border-color);
                border-radius: 4px;
                background: var(--bg-secondary) !important;
                transition: all 0.3s ease;
                padding: 10px !important;
            }
            .opblock-tag:hover {
                background: var(--bg-secondary) !important;
                opacity: 0.9;
            }
            .opblock-tag-description {
                font-weight: bold;
                color: var(--text-primary);
            }
            .controllers-section {
                display: none;
                margin-top: 10px;
                padding: 10px;
                border-top: 1px solid var(--border-color);
                font-size: 13px;
                color: var(--text-secondary);
            }
            .opblock-tag-section.is-open .controllers-section {
                display: block;
            }
            .controllers-title {
                font-weight: bold;
                margin-bottom: 8px;
            }
            .controllers-list {
                list-style: none;
                margin: 0;
                padding: 0;
            }
            .controllers-list li {
                margin: 4px 0;
                padding-left: 15px;
                position: relative;
            }
            .controllers-list li:before {
                content: "•";
                position: absolute;
                left: 0;
                color: var(--accent-color);
            }
        `;
        document.head.appendChild(style);

        // Ajouter la section des contrôleurs
        function addControllersSection(section) {
            const tag = section.querySelector('.opblock-tag');
            if (!tag) return;

            const tagData = window.swaggerData?.spec?.tags?.find(t => t.name === tag.getAttribute('data-tag'));
            if (!tagData?.['x-controllers']) return;

            const controllers = tagData['x-controllers'].split(',');
            if (!controllers.length) return;

            const controllersSection = document.createElement('div');
            controllersSection.className = 'controllers-section';
            controllersSection.innerHTML = `
                <div class="controllers-title">Available Controllers:</div>
                <ul class="controllers-list">
                    ${controllers.map(c => `<li>${c}</li>`).join('')}
                </ul>
            `;

            // Insérer après le tag mais avant les opérations
            const operations = section.querySelector('.no-margin');
            if (operations) {
                operations.parentNode.insertBefore(controllersSection, operations);
            } else {
                tag.parentNode.appendChild(controllersSection);
            }
        }

        // Initialiser les sections
        document.querySelectorAll('.opblock-tag-section').forEach(addControllersSection);

        // Observer les changements pour les nouvelles sections
        const observer = new MutationObserver(mutations => {
            mutations.forEach(mutation => {
                mutation.addedNodes.forEach(node => {
                    if (node.classList?.contains('opblock-tag-section')) {
                        addControllersSection(node);
                    }
                });
            });
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }, 1000);
}); 