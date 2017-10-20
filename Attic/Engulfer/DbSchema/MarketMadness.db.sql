-- phpMyAdmin SQL Dump
-- version 4.6.6deb4
-- https://www.phpmyadmin.net/
--
-- Host: localhost:3306
-- Generation Time: Aug 08, 2017 at 05:26 PM
-- Server version: 5.7.19
-- PHP Version: 7.0.18-0ubuntu0.17.04.1

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";

--
-- Database: `MarketMadness`
--

-- --------------------------------------------------------

--
-- Table structure for table `EodEntry`
--

CREATE TABLE `EodEntry` (
  `Id` int(11) NOT NULL,
  `Ticker` varchar(10) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Per` varchar(5) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Date` date NOT NULL,
  `Open` decimal(10,0) NOT NULL,
  `High` decimal(10,0) NOT NULL,
  `Low` decimal(10,0) NOT NULL,
  `Close` decimal(10,0) NOT NULL,
  `Vol` bigint(20) NOT NULL,
  `OI` bigint(20) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `EodEntry`
--
ALTER TABLE `EodEntry`
  ADD PRIMARY KEY (`Id`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `EodEntry`
--
ALTER TABLE `EodEntry`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=1;
