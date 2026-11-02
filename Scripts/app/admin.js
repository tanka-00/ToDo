(function ($) {
    'use strict';

    var $app = $('#adminApp');

    if (!$app.length) {
        return;
    }

    var urls = {
        users: $app.data('users-url'),
        toggle: $app.data('toggle-url'),
        tasks: $app.data('tasks-url')
    };

    var users = [];
    var usersById = {};
    var tasksPanel = null;

    function loadUsers() {
        $('#usersLoading').prop('hidden', false);
        $('#usersEmpty').prop('hidden', true);
        $('#userTableBody').empty();

        TodoApp.get(urls.users)
            .done(function (response) {
                if (!response || !response.success) {
                    showUsersError();
                    return;
                }

                users = response.users || [];
                renderUsers();
            })
            .fail(function (xhr) {
                showUsersError();
                TodoApp.handleError(xhr, 'Could not load users.');
            })
            .always(function () {
                $('#usersLoading').prop('hidden', true);
            });
    }

    function showUsersError() {
        $('#usersEmpty').find('h2').text('Could not load users.');
        $('#usersEmpty').find('p').text('Refresh the page and try again.');
        $('#usersEmpty').prop('hidden', false);
    }

    function renderUsers() {
        usersById = {};

        users.forEach(function (user) {
            usersById[user.id] = user;
        });

        updateStats();
        $('#userCountLabel').text(users.length + (users.length === 1 ? ' user' : ' users'));

        if (users.length === 0) {
            $('#usersEmpty').find('h2').text('No users yet.');
            $('#usersEmpty').find('p').text('When people create accounts, they will appear here.');
            $('#usersEmpty').prop('hidden', false);
            return;
        }

        $('#usersEmpty').prop('hidden', true);
        $('#userTableBody').html(users.map(userRow).join(''));
    }

    function updateStats() {
        var active = users.filter(function (user) { return user.isActive; }).length;
        var totalTasks = users.reduce(function (sum, user) { return sum + Number(user.totalTasks || 0); }, 0);
        var completed = users.reduce(function (sum, user) { return sum + Number(user.completedTasks || 0); }, 0);

        $('#statUsers').text(users.length);
        $('#statActive').text(active);
        $('#statTasks').text(totalTasks);
        $('#statCompleted').text(completed);
    }

    function userRow(user) {
        var name = TodoApp.escapeHtml(user.name);
        var email = TodoApp.escapeHtml(user.email);
        var initials = TodoApp.escapeHtml(user.initials);
        var created = TodoApp.escapeHtml(user.createdLabel);
        var totalTasks = Number(user.totalTasks || 0);
        var completedTasks = Number(user.completedTasks || 0);
        var percent = totalTasks === 0 ? 0 : Math.round((completedTasks / totalTasks) * 100);
        var statusClass = user.isActive ? 'is-active' : 'is-inactive';
        var statusLabel = user.isActive ? 'Active' : 'Paused';
        var toggleClass = user.isActive ? 'danger' : 'success';
        var toggleLabel = user.isActive ? 'Deactivate' : 'Activate';

        return '<tr data-id="' + user.id + '">' +
            '<td>' +
                '<div class="user-cell">' +
                    '<span class="avatar">' + initials + '</span>' +
                    '<div class="user-cell-copy">' +
                        '<span class="user-name">' + name + '</span>' +
                        '<span class="user-email">' + email + '</span>' +
                    '</div>' +
                '</div>' +
            '</td>' +
            '<td class="task-cell">' +
                '<div class="task-cell-line"><strong>' + totalTasks + '</strong><span>' + completedTasks + ' done</span></div>' +
                '<div class="mini-meter" aria-hidden="true"><span style="width: ' + percent + '%"></span></div>' +
            '</td>' +
            '<td class="joined-cell">' + created + '</td>' +
            '<td><span class="status-pill ' + statusClass + '">' + statusLabel + '</span></td>' +
            '<td>' +
                '<div class="table-actions">' +
                    '<button type="button" class="row-btn view-tasks" data-id="' + user.id + '" aria-label="View tasks for ' + name + '">View tasks</button>' +
                    '<button type="button" class="row-btn toggle-user ' + toggleClass + '" data-id="' + user.id + '" aria-label="' + toggleLabel + ' ' + name + '">' + toggleLabel + '</button>' +
                '</div>' +
            '</td>' +
        '</tr>';
    }

    function getTasksPanel() {
        if (!tasksPanel) {
            tasksPanel = bootstrap.Offcanvas.getOrCreateInstance(document.getElementById('userTasksPanel'));
        }

        return tasksPanel;
    }

    function viewTasks(userId) {
        var user = usersById[userId];

        if (!user) {
            return;
        }

        $('#tasksPanelTitle').text(user.name + "'s tasks");
        $('#tasksPanelMeta').text(user.email + ' · ' + (user.isActive ? 'Active' : 'Paused'));
        $('#adminTaskList').empty().prop('hidden', true);
        $('#adminTasksEmpty').prop('hidden', true);
        $('#adminTasksLoading').prop('hidden', false);

        getTasksPanel().show();

        TodoApp.get(urls.tasks, { userId: userId })
            .done(function (response) {
                if (!response || !response.success) {
                    TodoApp.showToast(response && response.message ? response.message : 'Could not load tasks.', 'error');
                    return;
                }

                renderTasks(response.tasks || []);
            })
            .fail(function (xhr) {
                TodoApp.handleError(xhr, 'Could not load this user\'s tasks.');
            })
            .always(function () {
                $('#adminTasksLoading').prop('hidden', true);
            });
    }

    function renderTasks(tasks) {
        if (!tasks.length) {
            $('#adminTasksEmpty').prop('hidden', false);
            return;
        }

        $('#adminTasksEmpty').prop('hidden', true);
        $('#adminTaskList').html(tasks.map(adminTaskRow).join('')).prop('hidden', false);
    }

    function adminTaskRow(task) {
        var title = TodoApp.escapeHtml(task.title);
        var description = TodoApp.escapeHtml(task.description);
        var created = TodoApp.escapeHtml(task.createdLabel);
        var statusLabel = task.isCompleted ? 'Done' : 'Open';
        var statusClass = task.isCompleted ? 'is-done' : 'is-open';
        var descriptionMarkup = description
            ? '<p>' + description + '</p>'
            : '<p class="empty-task-note">No notes</p>';

        return '<article class="admin-task-row' + (task.isCompleted ? ' is-complete' : '') + '">' +
            '<div class="admin-task-top">' +
                '<span class="status-pill ' + statusClass + '">' + statusLabel + '</span>' +
                '<span class="mono">Added ' + created + '</span>' +
            '</div>' +
            '<h3>' + title + '</h3>' +
            descriptionMarkup +
        '</article>';
    }

    function toggleUser(userId, $button) {
        var user = usersById[userId];

        if (!user) {
            return;
        }

        if (user.isActive) {
            TodoApp.confirm({
                title: 'Deactivate ' + user.name + '?',
                message: 'They will not be able to sign in or use the app until you activate them again.',
                confirmLabel: 'Deactivate',
                destructive: true,
                onConfirm: function () {
                    setUserStatus(user, false, $button);
                }
            });

            return;
        }

        setUserStatus(user, true, $button);
    }

    function setUserStatus(user, isActive, $button) {
        $button.prop('disabled', true);

        TodoApp.post(urls.toggle, {
            userId: user.id,
            isActive: isActive
        })
            .done(function (response) {
                if (!response || !response.success) {
                    TodoApp.showToast(response && response.message ? response.message : 'The user could not be updated.', 'error');
                    return;
                }

                TodoApp.showToast(response.message, 'success');
                loadUsers();
            })
            .fail(function (xhr) {
                TodoApp.handleError(xhr, 'The user could not be updated.');
            })
            .always(function () {
                $button.prop('disabled', false);
            });
    }

    $('#userTableBody').on('click', '.view-tasks', function () {
        viewTasks(parseInt($(this).data('id'), 10));
    });

    $('#userTableBody').on('click', '.toggle-user', function () {
        toggleUser(parseInt($(this).data('id'), 10), $(this));
    });

    loadUsers();
})(jQuery);
