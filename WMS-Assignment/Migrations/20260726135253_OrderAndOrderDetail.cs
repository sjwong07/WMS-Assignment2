using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS_Assignment.Migrations
{
    /// <inheritdoc />
    public partial class OrderAndOrderDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Both FailedLogin and LockoutEnd already exist in the database table
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}