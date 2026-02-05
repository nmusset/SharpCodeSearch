// @ts-nocheck
(function () {
    // Get VS Code API
    const vscode = acquireVsCodeApi();

    // DOM Elements
    const patternInput = document.getElementById('pattern-input');
    const searchButton = document.getElementById('search-button');
    const clearButton = document.getElementById('clear-button');
    const matchCaseCheckbox = document.getElementById('match-case');
    const wholeWordCheckbox = document.getElementById('whole-word');
    const resultsContainer = document.getElementById('results-container');
    const resultsCount = document.getElementById('results-count');
    const statusMessage = document.getElementById('status-message');
    const detailsPanel = document.getElementById('details-panel');
    const detailsContent = document.getElementById('details-content');
    const closeDetailsButton = document.getElementById('close-details');

    // State
    let currentResults = [];
    let selectedResultIndex = -1;

    // Initialize event listeners
    function init() {
        searchButton.addEventListener('click', handleSearch);
        clearButton.addEventListener('click', handleClear);
        closeDetailsButton.addEventListener('click', hideDetails);

        // Enable search on Enter key (Ctrl+Enter in textarea)
        patternInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && e.ctrlKey) {
                e.preventDefault();
                handleSearch();
            }
        });

        // Handle messages from extension
        window.addEventListener('message', handleMessage);
    }

    // Handle search button click
    function handleSearch() {
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
        showStatus('Searching...', 'info');
        setLoading(true);

        // Send search request to extension
        vscode.postMessage({
            type: 'search',
            pattern: pattern,
            options: {
                matchCase: matchCaseCheckbox.checked,
                wholeWord: wholeWordCheckbox.checked
            }
        });
    }

    // Handle clear button click
    function handleClear() {
        patternInput.value = '';
        clearResults();
        hideStatus();
        hideDetails();
        patternInput.focus();
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
            case 'searchError':
                handleSearchError(message.error);
                break;
            case 'searchProgress':
                showStatus(message.message, 'info');
                break;
        }
    }

    // Handle search results
    function handleSearchResults(results) {
        setLoading(false);
        currentResults = results || [];

        if (currentResults.length === 0) {
            showEmptyState();
            showStatus('No matches found', 'info');
            return;
        }

        hideStatus();
        displayResults(currentResults);
        updateResultsCount(currentResults.length);
    }

    // Handle search error
    function handleSearchError(error) {
        setLoading(false);
        showStatus(`Error: ${error}`, 'error');
        showEmptyState();
    }

    // Display results in the list
    function displayResults(results) {
        resultsContainer.innerHTML = '';

        results.forEach((result, index) => {
            const resultItem = createResultItem(result, index);
            resultsContainer.appendChild(resultItem);
        });
    }

    // Create a result item element
    function createResultItem(result, index) {
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
            // Only handle if not clicking on a clickable link
            if (!e.target.classList.contains('clickable')) {
                selectResult(index);
                showDetails(result);
            }
        });

        // Click on file or location navigates to source
        const navigateHandler = (e) => {
            e.stopPropagation();
            handleResultClick(index);
        };
        fileElement.addEventListener('click', navigateHandler);
        locationElement.addEventListener('click', navigateHandler);

        return item;
    }

    // Handle result item click
    function handleResultClick(index) {
        selectResult(index);
        const result = currentResults[index];

        // Show details panel
        showDetails(result);

        // Navigate to the match location in editor
        vscode.postMessage({
            type: 'navigateToMatch',
            file: result.file,
            line: result.line,
            column: result.column
        });
    }

    // Select a result item
    function selectResult(index) {
        // Deselect previous
        const previousSelected = resultsContainer.querySelector('.result-item.selected');
        if (previousSelected) {
            previousSelected.classList.remove('selected');
        }

        // Select new
        selectedResultIndex = index;
        const newSelected = resultsContainer.querySelector(`[data-index="${index}"]`);
        if (newSelected) {
            newSelected.classList.add('selected');
        }
    }

    // Show details panel
    function showDetails(result) {
        detailsContent.innerHTML = '';

        // File
        addDetailRow('File', result.file || 'Unknown');

        // Location
        addDetailRow('Location', `Line ${result.line || '?'}, Column ${result.column || '?'}`);

        // Matched Code
        addDetailRow('Matched Code', result.code || result.matchedText || 'N/A');

        // Placeholders (if any)
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
        currentResults = [];
        selectedResultIndex = -1;
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

    // Initialize the webview
    init();
})();
