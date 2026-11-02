(function ($) {
    'use strict';

    var $app = $('#userApp');

    if (!$app.length) {
        return;
    }

    var urls = {
        tasks: $app.data('tasks-url'),
        save: $app.data('save-url'),
        remove: $app.data('delete-url'),
        toggle: $app.data('toggle-url')
    };

    var tasks = [];
    var filter = 'all';
    var taskModal = null;

    function loadTasks() {
        $('#taskLoading').prop('hidden', false);
        $('#taskList').prop('hidden', true);
        $('#taskEmpty').prop('hidden', true);

        TodoApp.get(urls.tasks)
            .done(function (response) {
                if (!response || !response.success) {
                    showLoadError();
                    return;
                }

                tasks = response.tasks || [];
                render();
            })
            .fail(function (xhr) {
                showLoadError();
                TodoApp.handleError(xhr, 'Could not load your tasks.');
            })
            .always(function () {
                $('#taskLoading').prop('hidden', true);
            });
    }

    function showLoadError() {
        $('#emptyTitle').text('Could not load tasks.');
        $('#emptyCopy').text('Refresh the page and try again.');
        $('#emptyAddTask').prop('hidden', true);
        $('#taskEmpty').prop('hidden', false);
    }

    function render() {
        updateStats();
        updateCompletion();
        updateFilterCounts();
        renderList();
    }

    function updateStats() {
        var done = tasks.filter(function (task) { return task.isCompleted; }).length;
        var open = tasks.length - done;

        $('#statOpen').text(open);
        $('#statDone').text(done);
        $('#statTotal').text(tasks.length);
    }

    function updateCompletion() {
        var done = tasks.filter(function (task) { return task.isCompleted; }).length;
        var percent = tasks.length === 0 ? 0 : Math.round((done / tasks.length) * 100);

        $('#completionText').text(done + ' of ' + tasks.length + ' done');
        $('#completionBar').css('width', percent + '%');
        $('#completionMeter').attr('aria-valuenow', percent);
    }

    function updateFilterCounts() {
        var done = tasks.filter(function (task) { return task.isCompleted; }).length;

        $('[data-count="all"]').text(tasks.length);
        $('[data-count="open"]').text(tasks.length - done);
        $('[data-count="done"]').text(done);
    }

    function renderList() {
        var visible = tasks.filter(function (task) {
            if (filter === 'open') {
                return !task.isCompleted;
            }

            if (filter === 'done') {
                return task.isCompleted;
            }

            return true;
        });

        if (visible.length === 0) {
            $('#taskList').empty().prop('hidden', true);
            $('#emptyAddTask').prop('hidden', false);

            if (filter === 'open') {
                $('#emptyTitle').text('No open tasks.');
                $('#emptyCopy').text('Everything is done. Add something new when you are ready.');
            } else if (filter === 'done') {
                $('#emptyTitle').text('Nothing done yet.');
                $('#emptyCopy').text('Complete a task and it will show up here.');
            } else {
                $('#emptyTitle').text('Nothing here yet.');
                $('#emptyCopy').text('Add your first task and get the day moving.');
            }

            $('#taskEmpty').prop('hidden', false);
            return;
        }

        $('#taskEmpty').prop('hidden', true);
        $('#taskList').html(visible.map(taskRow).join('')).prop('hidden', false);
    }

    function taskRow(task) {
        var title = TodoApp.escapeHtml(task.title);
        var description = TodoApp.escapeHtml(task.description);
        var created = TodoApp.escapeHtml(task.createdLabel);
        var checked = task.isCompleted ? ' checked' : '';
        var checkLabel = task.isCompleted
            ? 'Reopen ' + title
            : 'Mark ' + title + ' as done';

        var descriptionMarkup = description
            ? '<p class="task-desc">' + description + '</p>'
            : '';

        return '<article class="task-row' + (task.isCompleted ? ' is-complete' : '') + '" data-id="' + task.id + '">' +
            '<div class="task-check-wrap">' +
                '<input type="checkbox" class="task-check" data-id="' + task.id + '"' + checked + ' aria-label="' + checkLabel + '" />' +
            '</div>' +
            '<div class="task-body">' +
                '<h3 class="task-title">' + title + '</h3>' +
                descriptionMarkup +
                '<p class="task-meta">Added ' + created + '</p>' +
            '</div>' +
            '<div class="task-actions">' +
                '<button type="button" class="icon-btn edit-task" data-id="' + task.id + '" aria-label="Edit task: ' + title + '">' +
                    '<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 20h9" /><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z" /></svg>' +
                '</button>' +
                '<button type="button" class="icon-btn danger delete-task" data-id="' + task.id + '" aria-label="Delete task: ' + title + '">' +
                    '<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 6h18M8 6V4h8v2M19 6l-1 14H6L5 6M10 11v5M14 11v5" /></svg>' +
                '</button>' +
            '</div>' +
        '</article>';
    }

    function openNewTask() {
        $('#taskForm')[0].reset();
        $('#TaskId').val(0);
        $('#TitleError').text('');
        $('#Description').val('');
        $('#descriptionCount').text('0');
        $('#taskModalEyebrow').text('New task');
        $('#taskModalLabel').text('What needs doing?');
        $('#saveTaskBtn .btn-label').text('Add task');
        getTaskModal().show();
    }

    function openEditTask(taskId) {
        var task = tasks.filter(function (item) { return item.id === taskId; })[0];

        if (!task) {
            return;
        }

        $('#taskForm')[0].reset();
        $('#TaskId').val(task.id);
        $('#Title').val(task.title);
        $('#Description').val(task.description || '');
        $('#descriptionCount').text((task.description || '').length);
        $('#TitleError').text('');
        $('#taskModalEyebrow').text('Edit task');
        $('#taskModalLabel').text('Make a change.');
        $('#saveTaskBtn .btn-label').text('Save changes');
        getTaskModal().show();
    }

    function getTaskModal() {
        if (!taskModal) {
            taskModal = bootstrap.Modal.getOrCreateInstance(document.getElementById('taskModal'));
        }

        return taskModal;
    }

    function saveTask(event) {
        event.preventDefault();

        var title = $.trim($('#Title').val());
        var description = $.trim($('#Description').val());
        var taskId = parseInt($('#TaskId').val(), 10) || 0;
        var $button = $('#saveTaskBtn');

        $('#TitleError').text('');

        if (!title) {
            $('#TitleError').text('Give your task a title.');
            $('#Title').trigger('focus');
            return;
        }

        TodoApp.setBusy($button, true);

        TodoApp.post(urls.save, {
            TaskId: taskId,
            Title: title,
            Description: description
        })
            .done(function (response) {
                if (!response || !response.success) {
                    var message = response && response.message ? response.message : 'The task could not be saved.';

                    if (/title/i.test(message)) {
                        $('#TitleError').text(message);
                    } else {
                        TodoApp.showToast(message, 'error');
                    }

                    return;
                }

                getTaskModal().hide();
                TodoApp.showToast(response.message, 'success');
                loadTasks();
            })
            .fail(function (xhr) {
                TodoApp.handleError(xhr, 'The task could not be saved.');
            })
            .always(function () {
                TodoApp.setBusy($button, false);
            });
    }

    function toggleTask() {
        var $checkbox = $(this);
        var taskId = parseInt($checkbox.data('id'), 10);
        var isCompleted = $checkbox.prop('checked');

        $checkbox.prop('disabled', true);

        TodoApp.post(urls.toggle, {
            taskId: taskId,
            isCompleted: isCompleted
        })
            .done(function (response) {
                if (!response || !response.success) {
                    $checkbox.prop('checked', !isCompleted);
                    TodoApp.showToast(response && response.message ? response.message : 'The task could not be updated.', 'error');
                    return;
                }

                TodoApp.showToast(response.message, 'success');
                loadTasks();
            })
            .fail(function (xhr) {
                $checkbox.prop('checked', !isCompleted);
                TodoApp.handleError(xhr, 'The task could not be updated.');
            })
            .always(function () {
                $checkbox.prop('disabled', false);
            });
    }

    function deleteTask(taskId) {
        var task = tasks.filter(function (item) { return item.id === taskId; })[0];

        if (!task) {
            return;
        }

        TodoApp.confirm({
            title: 'Delete task?',
            message: 'This will permanently remove "' + task.title + '".',
            confirmLabel: 'Delete task',
            destructive: true,
            onConfirm: function () {
                TodoApp.post(urls.remove, { taskId: taskId })
                    .done(function (response) {
                        if (!response || !response.success) {
                            TodoApp.showToast(response && response.message ? response.message : 'The task could not be deleted.', 'error');
                            return;
                        }

                        TodoApp.showToast(response.message, 'success');
                        loadTasks();
                    })
                    .fail(function (xhr) {
                        TodoApp.handleError(xhr, 'The task could not be deleted.');
                    });
            }
        });
    }

    $('#newTaskBtn, #emptyAddTask').on('click', openNewTask);

    $('#taskForm').on('submit', saveTask);

    $('#Description').on('input', function () {
        $('#descriptionCount').text($(this).val().length);
    });

    $('#taskList').on('change', '.task-check', toggleTask);

    $('#taskList').on('click', '.edit-task', function () {
        openEditTask(parseInt($(this).data('id'), 10));
    });

    $('#taskList').on('click', '.delete-task', function () {
        deleteTask(parseInt($(this).data('id'), 10));
    });

    $('.filter-chip').on('click', function () {
        filter = $(this).data('filter');

        $('.filter-chip').removeClass('is-active').attr('aria-pressed', 'false');
        $(this).addClass('is-active').attr('aria-pressed', 'true');
        renderList();
    });

    $('#taskModal').on('shown.bs.modal', function () {
        $('#Title').trigger('focus');
    });

    loadTasks();
})(jQuery);
