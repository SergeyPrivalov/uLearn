$(document).ready(function () {
    $('.antiplagiarism__data').each(function () {
        let $self = $(this);
        let originalSubmissionId = $self.data('originalSubmissionId');
        let plagiarismSubmissionId = $self.data('plagiarismSubmissionId');

        let $originalSubmission = $('.code[data-submission-id="' + originalSubmissionId + '"]');
        let $plagiarismSubmission = $('.code[data-submission-id="' + plagiarismSubmissionId + '"]');
        let originalCodeMirror = $originalSubmission[0].codeMirrorEditor;
        let plagiarismCodeMirror = $plagiarismSubmission[0].codeMirrorEditor;
        
        let antiplagiarismData = JSON.parse($self[0].innerHTML);       
        let plagiarismData = antiplagiarismData.plagiarism;

        let originalTokens = getTokensDictionaryByIndex(antiplagiarismData.tokens_positions);
        let plagiarismTokens = getTokensDictionaryByIndex(plagiarismData.tokens_positions);
       
        highlightNotAnalyzedParts(originalCodeMirror, antiplagiarismData.analyzed_code_units, originalTokens);
        highlightNotAnalyzedParts(plagiarismCodeMirror, plagiarismData.analyzed_code_units, plagiarismTokens);
        
        highlightMatchedTokens(plagiarismData.matched_snippets, originalCodeMirror, plagiarismCodeMirror, originalTokens, plagiarismTokens);
    });

    function getRandomColor() {
        // 30 random hues with step of 12 degrees
        let hue = Math.floor(Math.random() * 30) * 12;

        return $.Color({
            hue: hue,
            saturation: 0.9,
            lightness: 0.9,
            alpha: 0.5
        }).toRgbaString();
    }
    
    function getTokensDictionaryByIndex(tokensPositionsArray) {
        let result = {};
        $.each(tokensPositionsArray, function (idx, tokenInfo) {
            result[tokenInfo.token_index] = tokenInfo;
            result[tokenInfo.token_index].finish_position = tokenInfo.start_position + tokenInfo.length;
        });
        return result;        
    }
    
    function highlightNotAnalyzedParts(codeMirrorEditor, analyzedCodeUnits, tokens) {
        let document = codeMirrorEditor.getDoc();

        let highlightedTokes = [];
        $.each(analyzedCodeUnits, function (idx, codeUnit) {
            let firstTokenIndex = codeUnit.first_token_index;
            let lastTokenIndex = codeUnit.first_token_index + codeUnit.tokens_count - 1;
            for (let tokenIndex = firstTokenIndex; tokenIndex <= lastTokenIndex; tokenIndex++)
                highlightedTokes.push(tokenIndex);
        });
        highlightedTokes.sort();

        let textMarkerOptions = {
            className: 'antiplagiarism__not-analyzed',
            title: 'Эта часть кода не анализируется на списывание',
        };

        let currentHighlightStart = 0;
        for (let idx = 0; idx < highlightedTokes.length; idx++) {
            if (idx === 0 || highlightedTokes[idx - 1] < highlightedTokes[idx] - 1) {
                let currentHighlightFinish = tokens[highlightedTokes[idx]].start_position;
                if (currentHighlightStart !== currentHighlightFinish) {
                    document.markText(
                        document.posFromIndex(currentHighlightStart),
                        document.posFromIndex(currentHighlightFinish),
                        textMarkerOptions
                    );
                }
            }
            currentHighlightStart = tokens[highlightedTokes[idx]].finish_position;
        }

        document.markText(document.posFromIndex(currentHighlightStart), document.posFromIndex(1e10), textMarkerOptions);
    }    

    function highlightMatchedTokens(matchedSnippets, originalCodeMirror, plagiarismCodeMirror, originalTokens, plagiarismTokens) {
        highlightMatchedTokensInSubmission(matchedSnippets, originalCodeMirror, originalTokens, 'original_submission_first_token_index');
        highlightMatchedTokensInSubmission(matchedSnippets, plagiarismCodeMirror, plagiarismTokens, 'plagiarism_submission_first_token_index');
    }
    
    function highlightMatchedTokensInSubmission(matchedSnippets, codeMirrorEditor, tokens, firstTokenIndexSelector) {
        let tokensPlagiarismTypes = {};
        let maxTokenIndex = 0;
        $.each(matchedSnippets, function (idx, matchedSnippet) {
            let snippetType = matchedSnippet.snippet_type;
            for (let tokenIndex = matchedSnippet[firstTokenIndexSelector];
                 tokenIndex < matchedSnippet[firstTokenIndexSelector] + matchedSnippet.snippet_tokens_count;
                 tokenIndex++) {
                let oldValue = tokensPlagiarismTypes[tokenIndex];
                let newValue = snippetType;
                if (oldValue === undefined || (newValue = 'tokensKindsAndValues'))
                    tokensPlagiarismTypes[tokenIndex] = newValue;
                
                if (tokenIndex > maxTokenIndex)
                    maxTokenIndex = tokenIndex;
            }
        });

        let document = codeMirrorEditor.getDoc();
        let currentStart = 0, currentFinish = 0, currentPlagiarismType = '';

        let hightlightCurrentTokensSequence = function() {
            document.markText(
                document.posFromIndex(currentStart),
                document.posFromIndex(currentFinish),
                {
                    className: 'antiplagiarism__plagiarism-token-' + currentPlagiarismType,
                    title: currentPlagiarismType === 'tokensKindsAndValues'
                        ? 'Эта часть кода совпадает полностью' 
                        : 'Эта часть кода подозрительно похожа'
                }
            );
        };
        
        for (let tokenIndex = 0; tokenIndex <= maxTokenIndex; tokenIndex++) {
            if (! (tokenIndex in tokensPlagiarismTypes)) {
                hightlightCurrentTokensSequence();
            } else if (! (tokenIndex - 1 in tokensPlagiarismTypes)) {
                currentStart = tokens[tokenIndex].start_position;
                currentPlagiarismType = tokensPlagiarismTypes[tokenIndex];
            } else if (tokensPlagiarismTypes[tokenIndex] !== tokensPlagiarismTypes[tokenIndex - 1]) {
                hightlightCurrentTokensSequence();
                currentStart = tokens[tokenIndex].start_position;
                currentPlagiarismType = tokensPlagiarismTypes[tokenIndex];
            }
            
            if (tokenIndex in tokensPlagiarismTypes)
                currentFinish = tokens[tokenIndex].finish_position;
        }        

        hightlightCurrentTokensSequence();        
    }

    /* Fetching antiplagiarism status */
    $('.antiplagiarism-status').each(function () {
        let $self = $(this);
        let url = $self.data('antiplagiarismUrl');
        $.getJSON(url, function (data) {
            $self.addClass('found-level' + data.suspicion_level);
            let message = '';
            switch (data.suspicion_level)
            {
                case 0: message = 'похожих решений не найдено'; break;
                case 1: message = 'найдено {count} похожих решений других студентов. {link}'; break;
                case 2: message = 'найдено {count} похожих решений других студентов. {link}'; break;
            }
            message = message.replace('{count}', data.suspicious_submissions_count);
            message = message.replace('{link}', '<a href="' + $self.data('antiplagiarismDetailsUrl') + '" target="_blank">Посмотреть</a>');
            $self.html('Проверка на списывание: ' + message);
        });
    });
    
    /* Changing submission on panel */
    $('.antiplagiarism__submissions-panel [name="submissionId"]').change(function () {
        let $self = $(this);
        let $form = $self.closest('form');
        $form.submit();
    });
});