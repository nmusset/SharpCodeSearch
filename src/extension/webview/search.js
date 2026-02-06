// @ts-nocheck
(function () {
    try {
        console.log('Sharp Code Search: Script loaded');

        // VS Code API will be acquired later when needed
        let vscode = null;

        // DOM Elements - Initialize to null, will be set in init()
        let patternInput = null;
        let replaceInput = null;
        let searchButton = null;
        let previewButton = null;
        let applyButton = null;
        let clearButton = null;
        let matchCaseCheckbox = null;
        let wholeWordCheckbox = null;
        let resultsContainer = null;
        let resultsCount = null;
        let statusMessage = null;
        let detailsPanel = null;
        let detailsContent = null;
        let closeDetailsButton = null;
        let resultsModeSelector = null;
        let resultsModeButtons = null;

        // State
        let currentSearchResults = [];
        let currentReplacementResults = [];
        let currentApplicationResults = [];
        let currentMode = 'search'; // 'search', 'preview', 'applied'
        let selectedResultIndex = -1;

        // Initialize event listeners
        function init() {
            console.log('Init: Starting initialization...');
            
            try {
                // VS Code API should be acquired by inline script in HTML
                if (window.vscodeApi) {
                    console.log('Init: Using vscodeApi from window (inline script)');
                    vscode = window.vscodeApi;
                } else {
                    console.warn('Init: window.vscodeApi not found, trying acquireVsCodeAPI fallback...');
                    if (typeof acquireVsCodeAPI === 'function') {
                        console.log('Init: Calling acquireVsCodeAPI fallback...');
                        vscode = acquireVsCodeAPI();
                    } else {
                        throw new Error('Neither window.vscodeApi nor acquireVsCodeAPI available');
                    }
                }

                if (!vscode) {
                    throw new Error('Failed to get VS Code API (result was null/undefined)');
                }

                console.log('Init: VS Code API acquired successfully');
            } catch (e) {
                console.error('Init: Failed to acquire VS Code API:', e);
                document.body.innerHTML = '<div style="color: red; padding: 20px;"><h2>VS Code API Error</h2><p>Could not access VS Code API.</p><p>Error: ' + e.message + '</p><p>This may indicate a problem with the webview configuration.</p></div>';
                return;
            }

            // Get DOM elements
            patternInput = document.getElementById('pattern-input');
            replaceInput = document.getElementById('replace-input');
            searchButton = document.getElementById('search-button');
            previewButton = document.getElementById('preview-button');
            applyButton = document.getElementById('apply-button');
            clearButton = document.getElementById('clear-button');
            matchCaseCheckbox = document.getElementById('match-case');
            wholeWordCheckbox = document.getElementById('whole-word');
            resultsContainer = document.getElementById('results-container');
            resultsCount = document.getElementById('results-count');
            statusMessage = document.getElementById('status-message');
            detailsPanel = document.getElementById('details-panel');
            detailsContent = document.getElementById('details-content');
            closeDetailsButton = document.getElementById('close-details');
            resultsModeSelector = document.querySelector('.results-mode-selector');
            resultsModeButtons = document.querySelectorAll('.mode-button');

            console.log('Init: DOM elements loaded:', {
                patternInput: !!patternInput,
                replaceInput: !!replaceInput,
                searchButton: !!searchButton,
                previewButton: !!previewButton,
                applyButton: !!applyButton,
                clearButton: !!clearButton,
                resultsModeButtons: resultsModeButtons.length
            });

            console.log('Init: Attaching event listeners...');
        }
        
        if (clearButton) {
            clearButton.addEventListener('click', () => {
                console.log('Clear button clicked');
                handleClear();
            });
        }
        
        if (closeDetailsButton) {
            closeDetailsButton.addEventListener('click', hideDetails);
        }

        // Mode selector buttons
        if (resultsModeButtons && resultsModeButtons.length > 0) {
            resultsModeButtons.forEach(button => {
                button.addEventListener('click', (e) => {
                    console.log('Mode button clicked:', e.target.dataset.mode);
                    switchMode(e.target.dataset.mode);
                });
            });
        }

        // Enable search on Enter key (Ctrl+Enter in textarea)
        if (patternInput) {
            patternInput.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' && e.ctrlKey) {
                    e.preventDefault();
                    handleSearch();
                }
            });
        }

        if (replaceInput) {
            replaceInput.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' && e.ctrlKey) {
                    e.preventDefault();
                    if (!previewButton || !previewButton.disabled) {
                        handlePreview();
                    }
                }
            });
        }

        // Update UI when patterns change
        if (patternInput) {
            patternInput.addEventListener('change', updateUi);
        }
        if (replaceInput) {
            replaceInput.addEventListener('change', updateUi);
        }

        // Handle messages from extension
        window.addEventListener('message', handleMessage);
        
        console.log('Event listeners initialized successfully');
    }

    // Update UI button states
    function updateUi() {
        const hasPattern = patternInput.value.trim().length > 0;
        const hasReplacePattern = replaceInput.value.trim().length > 0;
        
        // Preview button enabled if we have results and replacement pattern
        previewButton.disabled = !(currentSearchResults.length > 0 && hasReplacePattern);
        
        // Apply button enabled if we have replacement previews
        applyButton.disabled = currentReplacementResults.length === 0;
    }

    // Handle search button click
    function handleSearch() {
        console.log('handleSearch called');
        const pattern = patternInput.value.trim();

        // Validate pattern
        if (!pattern) {
            showStatus('Please enter a search pattern', 'error');
            return;
        }

        if (!validatePattern(pattern)) {
            showStatus('Invalid pattern syntax. Use $name$ for placeholders.', 'error');
            return;
        }

        // Clear previous results
        clearResults();
        currentMode = 'search';
        resultsModeSelector.classList.add('hidden');
        showStatus('Searching...', 'info');
        setLoading(true);

        // Send search request to extension
        console.log('Posting search message:', { type: 'search', pattern });
        vscode.postMessage({
            type: 'search',
            pattern: pattern,
            options: {
                matchCase: matchCaseCheckbox.checked,
                wholeWord: wholeWordCheckbox.checked
            }
        });
    }

    // Handle preview button click
    function handlePreview() {
        const pattern = patternInput.value.trim();
        const replacePattern = replaceInput.value.trim();

        if (!pattern) {
            showStatus('Please enter a search pattern', 'error');
            return;
        }

        if (!replacePattern) {
            showStatus('Please enter a replacement pattern', 'error');
            return;
        }

        if (!validatePattern(pattern)) {
            showStatus('Invalid search pattern syntax', 'error');
            return;
        }

        if (!validatePattern(replacePattern)) {
            showStatus('Invalid replacement pattern syntax', 'error');
            return;
        }

        showStatus('Generating replacement preview...', 'info');
        previewButton.disabled = true;
        currentMode = 'preview';

        // Send replacement preview request
        vscode.postMessage({
            type: 'preview',
            pattern: pattern,
            replacePattern: replacePattern,
            options: {
                matchCase: matchCaseCheckbox.checked,
                wholeWord: wholeWordCheckbox.checked
            }
        });
    }

    // Handle apply button click
    async function handleApply() {
        const confirmMsg = `Apply ${currentReplacementResults.length} replacement(s) to ${new Set(currentReplacementResults.map(r => r.filePath)).size} file(s)?`;
        
        // Simple prompt for confirmation
        if (!confirm(confirmMsg)) {
            return;
        }

        const pattern = patternInput.value.trim();
        const replacePattern = replaceInput.value.trim();

        showStatus('Applying changes...', 'info');
        applyButton.disabled = true;

        // Send apply request
        vscode.postMessage({
            type: 'apply',
            pattern: pattern,
            replacePattern: replacePattern,
            options: {
                matchCase: matchCaseCheckbox.checked,
                wholeWord: wholeWordCheckbox.checked
            }
        });
    }

    // Handle clear button click
    function handleClear() {
        console.log('handleClear called');
        patternInput.value = '';
        replaceInput.value = '';
        clearResults();
        hideStatus();
        hideDetails();
        patternInput.focus();
        updateUi();
        console.log('Clear completed');
    }

    // Validate pattern syntax
    function validatePattern(pattern) {
        // Check for unmatched $ symbols
        const dollarCount = (pattern.match(/\$/g) || []).length;

        // If no $ symbols, it's a literal search (valid)
        if (dollarCount === 0) {
            return true;
        }

        // Must have even number of $ symbols
        if (dollarCount % 2 !== 0) {
            return false;
        }

        // Check that all $ pairs form valid placeholders
        const validPlaceholders = pattern.match(/\$[a-zA-Z_][a-zA-Z0-9_]*\$/g);
        const validDollarCount = validPlaceholders ? validPlaceholders.join('').match(/\$/g).length : 0;

        // If the count doesn't match, there are invalid $ pairs
        if (dollarCount !== validDollarCount) {
            return false;
        }

        return true;
    }

    // Handle messages from extension
    function handleMessage(event) {
        const message = event.data;

        switch (message.type) {
            case 'searchResults':
                handleSearchResults(message.results);
                break;
            case 'replacementResults':
                handleReplacementResults(message.results);
                break;
            case 'applicationResults':
                handleApplicationResults(message.results);
                break;
            case 'searchError':
                handleSearchError(message.error);
                break;
            case 'replacementError':
                handleReplacementError(message.error);
                break;
            case 'applicationError':
                handleApplicationError(message.error);
                break;
            case 'searchProgress':
                showStatus(message.message, 'info');
                break;
        }
    }

    // Handle search results
    function handleSearchResults(results) {
        setLoading(false);
        currentSearchResults = results || [];
        currentReplacementResults = [];
        currentApplicationResults = [];
        currentMode = 'search';
        resultsModeSelector.classList.add('hidden');

        if (currentSearchResults.length === 0) {
            showEmptyState();
            showStatus('No matches found', 'info');
            updateUi();
            return;
        }

        hideStatus();
        displaySearchResults(currentSearchResults);
        updateResultsCount(currentSearchResults.length);
        updateUi();
    }

    // Handle replacement preview results
    function handleReplacementResults(results) {
        previewButton.disabled = false;
        currentReplacementResults = results || [];
        currentApplicationResults = [];
        currentMode = 'preview';

        if (currentReplacementResults.length === 0) {
            showStatus('No replacements to preview', 'error');
            updateUi();
            return;
        }

        hideStatus();
        resultsModeSelector.classList.remove('hidden');
        updateModeSelectorButtons();
        displayReplacementComparison(currentReplacementResults);
        updateResultsCount(currentReplacementResults.length);
    }

    // Handle application results
    function handleApplicationResults(results) {
        applyButton.disabled = false;
        currentApplicationResults = results || [];
        currentMode = 'applied';

        if (currentApplicationResults.length === 0) {
            showStatus('Replacements completed (no changes applied)', 'info');
            updateUi();
            return;
        }

        const successCount = currentApplicationResults.filter(r => r.success).length;
        const errorCount = currentApplicationResults.filter(r => !r.success).length;

        if (errorCount > 0) {
            showStatus(`Applied to ${successCount} file(s), ${errorCount} error(s)`, 'error');
        } else {
            showStatus(`Successfully applied to ${successCount} file(s)!`, 'info');
        }

        resultsModeSelector.classList.remove('hidden');
        updateModeSelectorButtons();
        displayApplicationResults(currentApplicationResults);
        updateResultsCount(successCount);
    }

    // Handle errors
    function handleSearchError(error) {
        setLoading(false);
        showStatus(`Search error: ${error}`, 'error');
        showEmptyState();
        updateUi();
    }

    function handleReplacementError(error) {
        previewButton.disabled = false;
        showStatus(`Replacement error: ${error}`, 'error');
        updateUi();
    }

    function handleApplicationError(error) {
        applyButton.disabled = false;
        showStatus(`Application error: ${error}`, 'error');
        updateUi();
    }

    // Display search results
    function displaySearchResults(results) {
        resultsContainer.innerHTML = '';

        results.forEach((result, index) => {
            const resultItem = createSearchResultItem(result, index);
            resultsContainer.appendChild(resultItem);
        });
    }

    // Create a search result item element
    function createSearchResultItem(result, index) {
        const item = document.createElement('div');
        item.className = 'result-item';
        item.dataset.index = index;

        const fileElement = document.createElement('div');
        fileElement.className = 'result-file clickable';
        fileElement.textContent = result.file || 'Unknown file';

        const locationElement = document.createElement('div');
        locationElement.className = 'result-location clickable';
        locationElement.textContent = `Line ${result.line || '?'}, Column ${result.column || '?'}`;

        const codeElement = document.createElement('div');
        codeElement.className = 'result-code';
        codeElement.textContent = result.code || result.matchedText || 'No preview available';

        item.appendChild(fileElement);
        item.appendChild(locationElement);
        item.appendChild(codeElement);

        // Click on item shows details without navigation
        item.addEventListener('click', (e) => {
            if (!e.target.classList.contains('clickable')) {
                selectResult(index);
                showDetailsForSearch(result);
            }
        });

        // Click on file or location navigates to source
        const navigateHandler = (e) => {
            e.stopPropagation();
            selectResult(index);
            showDetailsForSearch(result);
            vscode.postMessage({
                type: 'navigateToMatch',
                file: result.file,
                line: result.line,
                column: result.column
            });
        };
        fileElement.addEventListener('click', navigateHandler);
        locationElement.addEventListener('click', navigateHandler);

        return item;
    }

    // Display replacement comparison
    function displayReplacementComparison(replacements) {
        resultsContainer.innerHTML = '';

        replacements.forEach((replacement, index) => {
            const resultItem = document.createElement('div');
            resultItem.className = 'result-item';
            resultItem.dataset.index = index;

            const fileElement = document.createElement('div');
            fileElement.className = 'result-file';
            fileElement.textContent = replacement.filePath || 'Unknown file';
            fileElement.style.cursor = 'pointer';
            fileElement.style.textDecoration = 'underline dotted';

            const locationElement = document.createElement('div');
            locationElement.className = 'result-location';
            locationElement.textContent = `Line ${replacement.line || '?'}, Column ${replacement.column || '?'}`;

            const comparisonElement = document.createElement('div');
            comparisonElement.className = 'result-comparison';

            const originalColumn = document.createElement('div');
            originalColumn.className = 'comparison-column';
            originalColumn.innerHTML = `
                <div class="comparison-title original">Original</div>
                <div class="comparison-code original">${escapeHtml(replacement.originalCode || '')}</div>
            `;

            const replacementColumn = document.createElement('div');
            replacementColumn.className = 'comparison-column';
            replacementColumn.innerHTML = `
                <div class="comparison-title replacement">Replacement</div>
                <div class="comparison-code replacement">${escapeHtml(replacement.replacementCode || '')}</div>
            `;

            comparisonElement.appendChild(originalColumn);
            comparisonElement.appendChild(replacementColumn);

            resultItem.appendChild(fileElement);
            resultItem.appendChild(locationElement);
            resultItem.appendChild(comparisonElement);

            // Navigate to match on click
            fileElement.addEventListener('click', () => {
                vscode.postMessage({
                    type: 'navigateToMatch',
                    file: replacement.filePath,
                    line: replacement.line,
                    column: replacement.column
                });
            });

            resultsContainer.appendChild(resultItem);
        });
    }

    // Display application results
    function displayApplicationResults(results) {
        resultsContainer.innerHTML = '';

        results.forEach((result, index) => {
            const resultItem = document.createElement('div');
            resultItem.className = 'result-item ' + (result.success ? '' : 'result-error');
            resultItem.dataset.index = index;

            const fileElement = document.createElement('div');
            fileElement.className = 'result-file';
            fileElement.innerHTML = (result.success ? '✓ ' : '✗ ') + (result.filePath || 'Unknown file');

            const statusElement = document.createElement('div');
            statusElement.className = 'result-location';
            if (result.success) {
                statusElement.textContent = `Applied ${result.replacementsApplied} replacement(s)`;
            } else {
                statusElement.textContent = `Error: ${result.error || 'Unknown error'}`;
            }

            resultItem.appendChild(fileElement);
            resultItem.appendChild(statusElement);
            resultsContainer.appendChild(resultItem);
        });
    }

    // Switch display mode
    function switchMode(mode) {
        currentMode = mode;
        updateModeSelectorButtons();

        switch (mode) {
            case 'search':
                displaySearchResults(currentSearchResults);
                updateResultsCount(currentSearchResults.length);
                break;
            case 'preview':
                displayReplacementComparison(currentReplacementResults);
                updateResultsCount(currentReplacementResults.length);
                break;
            case 'applied':
                displayApplicationResults(currentApplicationResults);
                const successCount = currentApplicationResults.filter(r => r.success).length;
                updateResultsCount(successCount);
                break;
        }
    }

    // Update which mode button is active
    function updateModeSelectorButtons() {
        resultsModeButtons.forEach(btn => {
            if (btn.dataset.mode === currentMode) {
                btn.classList.add('active');
            } else {
                btn.classList.remove('active');
            }
        });
    }

    // Select a result item
    function selectResult(index) {
        const previousSelected = resultsContainer.querySelector('.result-item.selected');
        if (previousSelected) {
            previousSelected.classList.remove('selected');
        }

        selectedResultIndex = index;
        const newSelected = resultsContainer.querySelector(`[data-index="${index}"]`);
        if (newSelected) {
            newSelected.classList.add('selected');
        }
    }

    // Show details panel for search result
    function showDetailsForSearch(result) {
        detailsContent.innerHTML = '';

        addDetailRow('File', result.file || 'Unknown');
        addDetailRow('Location', `Line ${result.line || '?'}, Column ${result.column || '?'}`);
        addDetailRow('Matched Code', result.code || result.matchedText || 'N/A');

        if (result.placeholders && Object.keys(result.placeholders).length > 0) {
            const placeholdersText = Object.entries(result.placeholders)
                .map(([key, value]) => `${key}: ${value}`)
                .join('\n');
            addDetailRow('Placeholders', placeholdersText);
        }

        detailsPanel.classList.remove('hidden');
    }

    // Add a detail row to the details panel
    function addDetailRow(label, value) {
        const row = document.createElement('div');
        row.className = 'detail-row';

        const labelElement = document.createElement('div');
        labelElement.className = 'detail-label';
        labelElement.textContent = label;

        const valueElement = document.createElement('div');
        valueElement.className = 'detail-value';
        valueElement.textContent = value;

        row.appendChild(labelElement);
        row.appendChild(valueElement);
        detailsContent.appendChild(row);
    }

    // Hide details panel
    function hideDetails() {
        detailsPanel.classList.add('hidden');
    }

    // Show empty state
    function showEmptyState() {
        resultsContainer.innerHTML = `
            <div class="empty-state">
                <span class="empty-icon">🔍</span>
                <p>No matches found</p>
            </div>
        `;
        updateResultsCount(0);
    }

    // Clear results
    function clearResults() {
        currentSearchResults = [];
        currentReplacementResults = [];
        currentApplicationResults = [];
        selectedResultIndex = -1;
        currentMode = 'search';
        resultsModeSelector.classList.add('hidden');
        resultsContainer.innerHTML = `
            <div class="empty-state">
                <span class="empty-icon">🔍</span>
                <p>Enter a pattern and click Search</p>
            </div>
        `;
        updateResultsCount(0);
        hideDetails();
    }

    // Update results count display
    function updateResultsCount(count) {
        resultsCount.textContent = `${count} ${count === 1 ? 'match' : 'matches'}`;
    }

    // Show status message
    function showStatus(message, type = 'info') {
        statusMessage.textContent = message;
        statusMessage.className = `status-message ${type}`;
        statusMessage.classList.remove('hidden');
    }

    // Hide status message
    function hideStatus() {
        statusMessage.classList.add('hidden');
    }

    // Set loading state
    function setLoading(isLoading) {
        searchButton.disabled = isLoading;
        if (isLoading) {
            searchButton.classList.add('loading');
        } else {
            searchButton.classList.remove('loading');
        }
    }

    // Escape HTML to prevent XSS
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Initialize the webview
    console.log('Sharp Code Search: Starting initialization...');
    init();
    console.log('Sharp Code Search: Initialization complete');
    } catch (error) {
        console.error('Sharp Code Search: Fatal error during initialization:', error);
        console.error('Stack trace:', error.stack);
        // Show error to user in the page
        document.body.innerHTML = '<div style="color: red; padding: 20px;"><h2>Error Loading Sharp Code Search</h2><p>' + error.message + '</p><pre>' + error.stack + '</pre></div>';
    }
})();

